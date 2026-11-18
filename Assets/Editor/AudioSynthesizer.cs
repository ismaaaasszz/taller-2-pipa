using System.IO;
using System.Text;
using UnityEngine;

/*
 * AudioSynthesizer.cs  (solo Editor)
 *
 * Genera por síntesis (código propio) los efectos de sonido y la música del juego
 * y los guarda como archivos WAV en Assets/Audio/SFX y Assets/Audio/Music.
 * Así todos los audios son originales y no dependen de licencias externas.
 * No se usa durante el juego: solo lo llama DinoCityRunBuilder.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public static class AudioSynthesizer
{
    const int SampleRate = 44100;                        // Muestras por segundo
    static System.Random random = new System.Random(1234); // Ruido con semilla fija (siempre igual)

    // Genera todos los audios del juego
    public static void GenerateAll(string sfxFolder, string musicFolder)
    {
        random = new System.Random(1234);
        WriteWav(sfxFolder + "/disparo.wav", Shoot());
        WriteWav(sfxFolder + "/dinosaurio_muere.wav", DinosaurDeath());
        WriteWav(sfxFolder + "/explosion.wav", Explosion());
        WriteWav(sfxFolder + "/moneda.wav", CoinPickup());
        WriteWav(sfxFolder + "/jugador_muere.wav", PlayerDeath());
        WriteWav(sfxFolder + "/click.wav", Click());
        WriteWav(musicFolder + "/musica_juego.wav", Song(140f, new[] { 45, 41, 48, 43, 45, 41, 48, 43 }, true, 0.5f));
        WriteWav(musicFolder + "/musica_menu.wav", Song(96f, new[] { 45, 41, 48, 43 }, false, 0.45f));
        WriteWav(musicFolder + "/musica_derrota.wav", GameOverMusic());
    }

    // Disparo: golpe de ruido corto + tono grave que cae
    static float[] Shoot()
    {
        float[] s = Buffer(0.22f);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            phase += Mathf.Lerp(420f, 90f, t / 0.22f) / SampleRate;
            s[i] = (Noise() * 0.8f + Square(phase) * 0.35f) * Mathf.Exp(-t * 28f);
        }
        return LowPass(s, 0.35f);
    }

    // Rugido del dinosaurio: diente de sierra grave que desciende con vibrato
    static float[] DinosaurDeath()
    {
        float duration = 1.2f;
        float[] s = Buffer(duration);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            float progress = t / duration;
            phase += Mathf.Lerp(190f, 55f, progress) * (1f + 0.08f * Mathf.Sin(t * 38f)) / SampleRate;
            float envelope = Mathf.Clamp01(t / 0.06f) * Mathf.Pow(1f - progress, 1.4f);
            s[i] = (Saw(phase) * 0.7f + Noise() * 0.35f) * envelope;
        }
        return LowPass(s, 0.18f);
    }

    // Explosión: ruido grave con caída larga
    static float[] Explosion()
    {
        float[] s = Buffer(0.9f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            s[i] = Noise() * Mathf.Exp(-t * 5f);
        }
        return Normalize(LowPass(LowPass(s, 0.08f), 0.12f), 0.9f);
    }

    // Moneda: dos notas agudas rápidas
    static float[] CoinPickup()
    {
        float[] s = Buffer(0.3f);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            phase += (t < 0.07f ? 988f : 1319f) / SampleRate;
            s[i] = Square(phase) * 0.35f * Mathf.Exp(-t * 9f);
        }
        return LowPass(s, 0.5f);
    }

    // Muerte del jugador: tono que baja con trémolo
    static float[] PlayerDeath()
    {
        float duration = 0.8f;
        float[] s = Buffer(duration);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            phase += Mathf.Lerp(620f, 110f, t / duration) / SampleRate;
            s[i] = Triangle(phase) * 0.7f * (0.6f + 0.4f * Mathf.Sin(t * 60f)) * (1f - t / duration);
        }
        return s;
    }

    // Clic de botón: pulso muy corto
    static float[] Click()
    {
        float[] s = Buffer(0.07f);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            phase += 900f / SampleRate;
            s[i] = Sine(phase) * 0.6f * Mathf.Exp(-T(i) * 60f);
        }
        return s;
    }

    // Música de derrota: melodía descendente en La menor con un bajo sostenido
    static float[] GameOverMusic()
    {
        int[] melody = { 69, 67, 65, 64, 62, 60, 59, 57 };
        float noteLength = 0.42f;
        float duration = melody.Length * noteLength + 1.6f;
        float[] s = Buffer(duration);
        float melodyPhase = 0f, dronePhase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            int note = Mathf.Min(melody.Length - 1, (int)(t / noteLength));
            float noteT = t - note * noteLength;
            melodyPhase += MidiToFreq(melody[note]) / SampleRate;
            dronePhase += MidiToFreq(33) / SampleRate;
            float noteEnvelope = Mathf.Clamp01(noteT / 0.02f) * Mathf.Exp(-noteT * (note == melody.Length - 1 ? 1.2f : 3f));
            float fadeOut = Mathf.Clamp01((duration - t) / 1.2f);
            s[i] = (Triangle(melodyPhase) * 0.5f * noteEnvelope + Saw(dronePhase) * 0.12f) * fadeOut;
        }
        return Normalize(LowPass(s, 0.3f), 0.7f);
    }

    /*
     * Song()
     *
     * Genera una canción en bucle: un compás por acorde (La menor - Fa - Do - Sol),
     * bajo en corcheas, arpegio en semicorcheas y batería opcional.
     */
    static float[] Song(float bpm, int[] roots, bool drums, float volume)
    {
        float beat = 60f / bpm;
        float bar = beat * 4f;
        float[] s = Buffer(bar * roots.Length);
        float bassPhase = 0f, arpPhase = 0f, kickPhase = 0f;

        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            int barIndex = Mathf.Min(roots.Length - 1, (int)(t / bar));
            int root = roots[barIndex];
            int third = barIndex % 4 == 0 ? 3 : 4; // Acorde menor en La, mayores en el resto

            float eighth = beat * 0.5f;
            float eighthT = t % eighth;
            bassPhase += MidiToFreq(root) / SampleRate;
            float sample = Square(bassPhase) * 0.22f * Mathf.Exp(-eighthT * 6f);

            float sixteenth = beat * 0.25f;
            int step = (int)(t / sixteenth);
            int[] chord = { 0, third, 7 };
            int arpNote = root + 24 + chord[step % 3] + (step % 8 >= 6 ? 12 : 0);
            arpPhase += MidiToFreq(arpNote) / SampleRate;
            sample += Triangle(arpPhase) * 0.16f * Mathf.Exp(-(t % sixteenth) * 10f);

            float beatT = t % beat;
            int beatIndex = (int)(t / beat) % 4;
            if (drums)
            {
                if (beatIndex == 0 || beatIndex == 2)
                {
                    kickPhase += Mathf.Lerp(120f, 45f, Mathf.Clamp01(beatT / 0.12f)) / SampleRate;
                    sample += Sine(kickPhase) * 0.55f * Mathf.Exp(-beatT * 14f);
                }
                else
                {
                    sample += Noise() * 0.22f * Mathf.Exp(-beatT * 22f);
                }
                sample += Noise() * 0.05f * Mathf.Exp(-eighthT * 60f);
            }
            else if (beatIndex == 0)
            {
                kickPhase += Mathf.Lerp(90f, 40f, Mathf.Clamp01(beatT / 0.15f)) / SampleRate;
                sample += Sine(kickPhase) * 0.3f * Mathf.Exp(-beatT * 8f);
            }

            s[i] = sample;
        }
        return Normalize(LowPass(s, 0.45f), volume);
    }

    // ---------- Utilidades de síntesis ----------
    static float[] Buffer(float seconds) { return new float[Mathf.CeilToInt(seconds * SampleRate)]; }
    static float T(int i) { return (float)i / SampleRate; }
    static float Noise() { return (float)(random.NextDouble() * 2.0 - 1.0); }
    static float Sine(float p) { return Mathf.Sin(p * Mathf.PI * 2f); }
    static float Square(float p) { return (p % 1f) < 0.5f ? 1f : -1f; }
    static float Saw(float p) { return (p % 1f) * 2f - 1f; }
    static float Triangle(float p) { return Mathf.Abs((p % 1f) * 4f - 2f) - 1f; }
    static float MidiToFreq(int note) { return 440f * Mathf.Pow(2f, (note - 69) / 12f); }

    // Filtro pasa bajos simple (suaviza el sonido)
    static float[] LowPass(float[] s, float alpha)
    {
        float previous = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            previous += alpha * (s[i] - previous);
            s[i] = previous;
        }
        return s;
    }

    // Ajusta el volumen para que el pico sea "peak"
    static float[] Normalize(float[] s, float peak)
    {
        float max = 0.0001f;
        foreach (float v in s) max = Mathf.Max(max, Mathf.Abs(v));
        for (int i = 0; i < s.Length; i++) s[i] *= peak / max;
        return s;
    }

    // Guarda las muestras en un archivo WAV (PCM 16 bits, mono)
    static void WriteWav(string assetPath, float[] samples)
    {
        using (BinaryWriter w = new BinaryWriter(File.Create(Path.Combine(Directory.GetCurrentDirectory(), assetPath))))
        {
            int dataLength = samples.Length * 2;
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + dataLength); w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(SampleRate); w.Write(SampleRate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data")); w.Write(dataLength);
            foreach (float v in samples) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * short.MaxValue));
        }
    }
}

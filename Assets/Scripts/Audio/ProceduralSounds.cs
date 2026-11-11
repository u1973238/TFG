using UnityEngine;

// Sons provisionals generats per codi, perquè el joc tingui so encara que no hi hagi arxius d'àudio.
// Quan es posi un arxiu real amb el mateix nom a Assets/Resources/Audio/, l'AudioManager el farà servir en lloc d'aquests
public static class ProceduralSounds
{
    private const int SampleRate = 22050;
    private static readonly System.Random random = new System.Random(1234);

    // Alguns sons tenen diverses variacions perquè no soni sempre igual (les passes de l'aranya)
    public static AudioClip[] CreateVariations(string clipName)
    {
        int count = clipName == "step" ? 6 : clipName == "land" ? 4 : clipName == "jump" ? 3 : 0;
        if (count > 0)
        {
            AudioClip[] variations = new AudioClip[count];
            for (int i = 0; i < count; i++) variations[i] = Create(clipName);
            return variations;
        }

        AudioClip single = Create(clipName);
        return single != null ? new[] { single } : new AudioClip[0];
    }

    public static AudioClip Create(string clipName)
    {
        switch (clipName)
        {
            case "step": return Skitter(clipName);
            case "grab": return Noise(clipName, 0.05f, 90f, 0.4f, 0.3f);
            case "jump": return SpiderJump(clipName);
            case "land": return SpiderLand(clipName);
            case "plate": return Thump(clipName, 0.15f, 120f, 0.6f);
            case "web_shoot": return Thwip(clipName, 0.18f);
            case "web_attach": return Sweep(clipName, 0.09f, 950f, 900f, 45f, 0.4f);
            case "web_release": return Sweep(clipName, 0.1f, 600f, 350f, 35f, 0.3f);
            case "bridge": return Zip(clipName, 0.35f);
            case "talk": return Sweep(clipName, 0.12f, 600f, 900f, 18f, 0.3f);
            case "ui_click": return Sweep(clipName, 0.04f, 1800f, 1700f, 120f, 0.3f);
            case "door": return Rumble(clipName, 0.7f);
            case "lever": return Lever(clipName);
            case "collect": return Notes(clipName, 0.1f, 0.35f, 1046.5f, 1568f);
            case "race_start": return Notes(clipName, 0.12f, 0.2f, 523.3f, 523.3f, 1046.5f);
            case "mission_start": return Notes(clipName, 0.1f, 0.35f, 523.3f, 659.3f, 784f);
            case "mission_complete": return Notes(clipName, 0.09f, 0.45f, 523.3f, 659.3f, 784f, 1046.5f);
            case "victory": return Notes(clipName, 0.14f, 0.7f, 523.3f, 659.3f, 784f, 1046.5f, 784f, 1046.5f);
            case "fail": return Notes(clipName, 0.18f, 0.4f, 392f, 311.1f);
            case "bounce": return Sweep(clipName, 0.3f, 180f, 520f, 9f, 0.5f);
            case "fly_caught": return Sweep(clipName, 0.25f, 900f, 300f, 10f, 0.35f);
            case "buzz": return Buzz(clipName, 1f);
            case "ambience": return Ambience(clipName, 12f);
            case "music": return Music(clipName);
            default: return null;
        }
    }

    // --- Efectes curts ---

    // Passes d'aranya: 3 o 4 tocs molt petits i aguts, gairebé seguits, com potes primes que toquen el terra
    private static AudioClip Skitter(string clipName)
    {
        float[] data = new float[Samples(0.1f)];
        AddTaps(data, 3 + random.Next(2), 0.004f, 0.065f, 2600f, 4800f, 0.4f); // Cada pota sona una mica diferent
        return Build(clipName, data);
    }

    // Salt d'aranya: les potes empenyen (un parell de tocs petits) i un «fiu» suau i curt de l'aire
    private static AudioClip SpiderJump(string clipName)
    {
        float duration = 0.12f;
        float[] data = new float[Samples(duration)];
        AddTaps(data, 2, 0f, 0.012f, 3000f, 4200f, 0.35f);

        float low = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float white = White();
            low = Mathf.Lerp(low, white, 0.25f);
            // L'aire es fa més agut a mesura que l'aranya surt disparada
            float whoosh = (white - low) * Mathf.Sin(Mathf.PI * t / duration) * Mathf.Lerp(0.05f, 0.18f, t / duration);
            data[i] += whoosh;
        }
        return Build(clipName, data);
    }

    // Aterratge d'aranya: totes les potes toquen terra gairebé alhora i el cos fa un copet molt suau
    private static AudioClip SpiderLand(string clipName)
    {
        float[] data = new float[Samples(0.09f)];
        AddTaps(data, 5 + random.Next(3), 0f, 0.022f, 2200f, 4000f, 0.4f);

        float low = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            low = Mathf.Lerp(low, White(), 0.2f);
            float body = Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 70f) * 0.12f;
            data[i] += body + low * Mathf.Exp(-t * 90f) * 0.1f;
        }
        return Build(clipName, data);
    }

    // Afegeix uns quants tocs de pota (clic agut + una mica de fregament) repartits en un interval de temps
    private static void AddTaps(float[] data, int taps, float from, float spread, float minHz, float maxHz, float volume)
    {
        for (int tap = 0; tap < taps; tap++)
        {
            int start = Samples(from + Random01() * spread);
            float pitch = Mathf.Lerp(minHz, maxHz, Random01());
            float loudness = (0.5f + Random01() * 0.5f) * volume;
            float low = 0f;
            for (int i = 0; i < Samples(0.01f) && start + i < data.Length; i++)
            {
                float t = (float)i / SampleRate;
                float white = White();
                low = Mathf.Lerp(low, white, 0.35f);
                float click = Mathf.Sin(2f * Mathf.PI * pitch * t) * Mathf.Exp(-t * 900f);
                float scratch = (white - low) * Mathf.Exp(-t * 450f) * 0.6f;
                data[start + i] += (click + scratch) * loudness;
            }
        }
    }

    // Soroll suau que s'apaga ràpid (passes, agafar-se a la paret)
    private static AudioClip Noise(string clipName, float duration, float decay, float smoothing, float volume)
    {
        float[] data = new float[Samples(duration)];
        float filtered = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            filtered = Mathf.Lerp(filtered, White(), smoothing);
            data[i] = filtered * Mathf.Exp(-t * decay) * volume;
        }
        return Build(clipName, data);
    }

    // To que canvia de freqüència (salt, clics, enganxar el fil)
    private static AudioClip Sweep(string clipName, float duration, float fromHz, float toHz, float decay, float volume)
    {
        float[] data = new float[Samples(duration)];
        float phase = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float frequency = Mathf.Lerp(fromHz, toHz, t / duration);
            phase += 2f * Mathf.PI * frequency / SampleRate;
            data[i] = Mathf.Sin(phase) * Mathf.Exp(-t * decay) * volume;
        }
        return Build(clipName, data);
    }

    // Cop greu (aterrar, placa de pressió)
    private static AudioClip Thump(string clipName, float duration, float frequency, float volume)
    {
        float[] data = new float[Samples(duration)];
        float filtered = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            filtered = Mathf.Lerp(filtered, White(), 0.1f);
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 22f);
            data[i] = (tone * 0.8f + filtered * Mathf.Exp(-t * 35f) * 0.5f) * volume;
        }
        return Build(clipName, data);
    }

    // «Fiu» de la teranyina en sortir disparada
    private static AudioClip Thwip(string clipName, float duration)
    {
        float[] data = new float[Samples(duration)];
        float low = 0f;
        float phase = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float white = White();
            low = Mathf.Lerp(low, white, 0.15f);
            phase += 2f * Mathf.PI * Mathf.Lerp(1400f, 500f, t / duration) / SampleRate;
            data[i] = ((white - low) * 0.5f + Mathf.Sin(phase) * 0.15f) * Mathf.Exp(-t * 20f);
        }
        return Build(clipName, data);
    }

    // So de fil que s'estira (crear un pont)
    private static AudioClip Zip(string clipName, float duration)
    {
        float[] data = new float[Samples(duration)];
        float low = 0f;
        float phase = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float white = White();
            low = Mathf.Lerp(low, white, 0.2f);
            phase += 2f * Mathf.PI * Mathf.Lerp(400f, 900f, t / duration) / SampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t / duration);
            data[i] = ((white - low) * 0.3f + Mathf.Sin(phase) * 0.12f) * envelope;
        }
        return Build(clipName, data);
    }

    // Palanca: uns quants clics del mecanisme i un cop sec de fusta quan el mànec arriba a baix
    private static AudioClip Lever(string clipName)
    {
        float[] data = new float[Samples(0.3f)];
        AddTaps(data, 3, 0f, 0.1f, 1700f, 2500f, 0.4f);

        int start = Samples(0.13f);
        float low = 0f;
        for (int i = 0; start + i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            low = Mathf.Lerp(low, White(), 0.25f);
            float knock = Mathf.Sin(2f * Mathf.PI * 210f * t) * Mathf.Exp(-t * 45f);
            data[start + i] += (knock * 0.7f + low * Mathf.Exp(-t * 70f) * 0.5f) * 0.8f;
        }
        return Build(clipName, data);
    }

    // Retruny greu (porta que es mou)
    private static AudioClip Rumble(string clipName, float duration)
    {
        float[] data = new float[Samples(duration)];
        float low = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            low = Mathf.Lerp(low, White(), 0.05f);
            float envelope = Mathf.Sin(Mathf.PI * t / duration);
            data[i] = (low * 1.6f + Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.25f) * envelope * 0.6f;
        }
        return Build(clipName, data);
    }

    // Petita melodia de notes polsades (recollir, missions, victòria)
    private static AudioClip Notes(string clipName, float noteGap, float noteLength, params float[] frequencies)
    {
        float duration = noteGap * (frequencies.Length - 1) + noteLength;
        float[] data = new float[Samples(duration)];
        for (int n = 0; n < frequencies.Length; n++)
        {
            AddPluck(data, Samples(noteGap * n), frequencies[n], noteLength, 0.3f, false);
        }
        return Build(clipName, data);
    }

    // --- Sons llargs en bucle ---

    // Brunzit de mosca: to aspre que tremola una mica (es repeteix sense talls perquè el període és exacte)
    private static AudioClip Buzz(string clipName, float duration)
    {
        float[] data = new float[Samples(duration)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float wobble = Mathf.Sin(2f * Mathf.PI * 6f * t) * 0.15f;
            float phase = 2f * Mathf.PI * 190f * t + wobble;
            // Ona en dent de serra suavitzada (sumant harmònics)
            float saw = Mathf.Sin(phase) + 0.5f * Mathf.Sin(2f * phase) + 0.33f * Mathf.Sin(3f * phase) + 0.25f * Mathf.Sin(4f * phase);
            data[i] = saw * (0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 3f * t)) * 0.18f;
        }
        return Build(clipName, data);
    }

    // Vent suau de bosc amb algun ocell de tant en tant
    private static AudioClip Ambience(string clipName, float duration)
    {
        int length = Samples(duration);
        int fade = Samples(1f);
        float[] raw = new float[length + fade];
        float brown = 0f;
        float low = 0f;
        for (int i = 0; i < raw.Length; i++)
        {
            float t = (float)i / SampleRate;
            brown = Mathf.Clamp(brown * 0.995f + White() * 0.05f, -1f, 1f);
            low = Mathf.Lerp(low, brown, 0.05f);
            float gust = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / (duration / 3f));
            raw[i] = low * gust * 0.6f;
        }

        // Fem que el final enllaci amb el principi perquè el bucle no faci cap clic
        float[] data = new float[length];
        for (int i = 0; i < length; i++) data[i] = raw[i];
        for (int i = 0; i < fade; i++)
        {
            float k = (float)i / fade;
            data[i] = raw[i] * k + raw[length + i] * (1f - k);
        }

        // Ocells: piulets curts i fluixos
        for (int b = 0; b < 5; b++)
        {
            int start = Samples(Random01() * (duration - 0.5f));
            float frequency = 2400f + Random01() * 1200f;
            for (int c = 0; c < 2; c++) AddChirp(data, start + Samples(0.12f * c), frequency, 0.08f, 0.06f);
        }
        return Build(clipName, data);
    }

    // Música tranquil·la: arpegis amb els acords Do - Sol - La menor - Fa, en bucle
    private static AudioClip Music(string clipName)
    {
        const float beat = 0.625f; // 96 pulsacions per minut
        float[][] chords =
        {
            new[] { 261.6f, 329.6f, 392.0f, 523.3f }, // Do
            new[] { 196.0f, 246.9f, 293.7f, 392.0f }, // Sol
            new[] { 220.0f, 261.6f, 329.6f, 440.0f }, // La menor
            new[] { 174.6f, 220.0f, 261.6f, 349.2f }, // Fa
        };
        int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 1 }; // Ordre de les notes de cada compàs (corxeres)

        int bars = 8;
        float[] data = new float[Samples(bars * 4 * beat)];
        for (int bar = 0; bar < bars; bar++)
        {
            float[] chord = chords[bar % chords.Length];
            float barStart = bar * 4 * beat;

            // Baix: la fonamental una octava avall, a la primera i a la tercera pulsació
            AddPluck(data, Samples(barStart), chord[0] * 0.5f, 1.2f, 0.22f, true);
            AddPluck(data, Samples(barStart + 2 * beat), chord[0] * 0.5f, 1.2f, 0.16f, true);

            for (int step = 0; step < pattern.Length; step++)
            {
                AddPluck(data, Samples(barStart + step * beat * 0.5f), chord[pattern[step]], 0.9f, 0.1f, true);
            }
        }
        return Build(clipName, data);
    }

    // --- Ajudes ---

    // Nota polsada (com una corda): fonamental i dos harmònics que s'apaguen. Si wrap és cert, la cua torna a l'inici (per als bucles)
    private static void AddPluck(float[] data, int start, float frequency, float length, float volume, bool wrap)
    {
        int count = Samples(length);
        for (int i = 0; i < count; i++)
        {
            int index = start + i;
            if (index >= data.Length)
            {
                if (!wrap) break;
                index %= data.Length;
            }

            float t = (float)i / SampleRate;
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * t)
                       + 0.3f * Mathf.Sin(4f * Mathf.PI * frequency * t)
                       + 0.1f * Mathf.Sin(6f * Mathf.PI * frequency * t);
            float attack = Mathf.Clamp01(t / 0.005f);
            data[index] += tone * attack * Mathf.Exp(-t * 4.5f / length) * volume;
        }
    }

    private static void AddChirp(float[] data, int start, float frequency, float length, float volume)
    {
        int count = Samples(length);
        float phase = 0f;
        for (int i = 0; i < count && start + i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            phase += 2f * Mathf.PI * frequency * (1f + 0.4f * t / length) / SampleRate;
            data[start + i] += Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t / length) * volume;
        }
    }

    private static AudioClip Build(string clipName, float[] data)
    {
        // Evitem que cap mostra passi de 1 (soroll distorsionat)
        float peak = 0f;
        foreach (float sample in data) peak = Mathf.Max(peak, Mathf.Abs(sample));
        if (peak > 0.95f)
        {
            float scale = 0.95f / peak;
            for (int i = 0; i < data.Length; i++) data[i] *= scale;
        }

        AudioClip clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static int Samples(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
    private static float White() => (float)(random.NextDouble() * 2.0 - 1.0);
    private static float Random01() => (float)random.NextDouble();
}

using UnityEngine;

// Efectes visuals petits fets per codi (sense prefabs): núvols de pols en aterrar, de seda quan el fil s'enganxa,
// espurnes en recollir objectes... Cada efecte és un sistema de partícules que es destrueix sol en acabar
public static class Effects
{
    private static Material particleMaterial;

    // Petit núvol de partícules que surt en la direcció de la normal (per exemple, cap amunt des del terra)
    public static void Puff(Vector3 position, Vector3 normal, Color color, int count = 10, float size = 0.35f, float speed = 2f)
    {
        GameObject effect = new GameObject("Efecte_Nuvol");
        effect.transform.position = position;
        effect.transform.rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up);

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.gravityModifier = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 55f;
        shape.radius = size * 0.5f;

        // Les partícules creixen una mica i s'esvaeixen
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = GetMaterial();

        particles.Play();
    }

    // Material de partícula: una taca rodona i difuminada feta per codi, amb el mateix shader que els fils
    private static Material GetMaterial()
    {
        if (particleMaterial != null) return particleMaterial;

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }
        texture.Apply();

        particleMaterial = new Material(Shader.Find("Sprites/Default"));
        particleMaterial.mainTexture = texture;
        return particleMaterial;
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;

// Configura sola la importació dels sons d'Assets/Resources/Audio:
// la música i l'ambient (llargs) es llegeixen en streaming perquè no ocupin molta memòria,
// i els efectes curts es descomprimeixen en carregar-se perquè sonin a l'instant
public class AudioImportSettings : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.Replace('\\', '/').Contains("Resources/Audio/")) return;

        AudioImporter importer = (AudioImporter)assetImporter;
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        bool isLong = fileName == "music" || fileName == "ambience";

        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.loadType = isLong ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.quality = isLong ? 0.6f : 0.8f;
        importer.defaultSampleSettings = settings;

        // Els efectes no necessiten estèreo
        importer.forceToMono = !isLong;
    }
}

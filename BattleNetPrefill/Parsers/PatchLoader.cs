namespace BattleNetPrefill.Parsers
{
    public class PatchLoader
    {
        private readonly CdnRequestManager _cdnRequestManager;

        public PatchLoader(CdnRequestManager cdnRequestManager)
        {
            _cdnRequestManager = cdnRequestManager;
        }

        public async Task HandlePatchesAsync(BuildConfigFile buildConfig, TactProduct targetProduct, CDNConfigFile cdnConfig)
        {
            // For whatever reason, these products do not actually make this request
            var patchConfigExclusions = new List<TactProduct>
            {
                TactProduct.CodVanguard, TactProduct.CodModernWarfare, TactProduct.CodBOCW, TactProduct.Hearthstone,
                TactProduct.BlizzardArcadeCollection
            };
            if (buildConfig.patchConfig != null && !patchConfigExclusions.Contains(targetProduct))
            {
                _cdnRequestManager.QueueRequest(RootFolder.config, buildConfig.patchConfig.Value);
            }

            if (buildConfig.patch != null)
            {
                // Downloads the patch since the real client does, but we don't use it for anything.
                await _cdnRequestManager.GetRequestAsBytesAsync(RootFolder.patch, buildConfig.patch.Value);
            }

            // Unused by Hearthstone
            if (cdnConfig.patchFileIndex != null && targetProduct != TactProduct.Hearthstone && targetProduct != TactProduct.BlizzardArcadeCollection)
            {
                _cdnRequestManager.QueueRequest(RootFolder.patch, cdnConfig.patchFileIndex.Value, 0, cdnConfig.patchFileIndexSize - 1, isIndex: true);
            }

            if (buildConfig.patchIndex != null)
            {
                var upperByteRange = Math.Max(4095, buildConfig.patchIndexSize[1] - 1);
                _cdnRequestManager.QueueRequest(RootFolder.data, buildConfig.patchIndex[1], 0, upperByteRange);
            }

            // Unused by Hearthstone
            if (cdnConfig.patchArchives != null && targetProduct != TactProduct.Hearthstone && targetProduct != TactProduct.BlizzardArcadeCollection)
            {
                for (var i = 0; i < cdnConfig.patchArchives.Length; i++)
                {
                    var patchIndex = cdnConfig.patchArchives[i];
                    _cdnRequestManager.QueueRequest(RootFolder.patch, patchIndex, 0, cdnConfig.patchArchivesIndexSize[i] - 1, isIndex: true);
                }
            }
        }
    }
}
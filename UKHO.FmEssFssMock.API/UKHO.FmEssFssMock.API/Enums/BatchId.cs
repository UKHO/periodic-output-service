using System.ComponentModel;

namespace UKHO.FmEssFssMock.Enums
{
    public enum BatchId
    {
        [Description("2270f318-639c-4e64-a0c0-caddd5f4eb05")]
        EssFullAvcsBatch = 1,

        [Description("f9523d33-ef12-4cc1-969d-8a95f094a48b")]
        PosFullAvcsIsoSha1Batch = 2,

        [Description("483aa1b9-8a3b-49f2-bae9-759bb93b04d1")]
        PosFullAvcsZipBatch = 3,

        [Description("90fcdfa0-8229-43d5-b059-172491e5402b")]
        EssUpdateBatch = 4,

        [Description("d3871589-c0ea-4967-8537-63089e2398af")]
        PosUpdateZipBatch = 5,

        [Description("bece0a26-867c-4ea6-8ece-98afa246a00e")]
        PosFmCatalogueFileBatch = 6,

        [Description("472b599d-494d-4ac5-a281-91e3927b24d4")]
        PosEncUpdatesFileBatch = 7
    }
}

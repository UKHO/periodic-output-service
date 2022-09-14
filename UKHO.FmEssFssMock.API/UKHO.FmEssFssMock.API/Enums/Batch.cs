using System.ComponentModel;

namespace UKHO.FmEssFssMock.Enums
{
    public enum Batch
    {
        [Description("2270f318-639c-4e64-a0c0-caddd5f4eb05")]
        EssFullAvcsZipBatch = 1,

        [Description("f9523d33-ef12-4cc1-969d-8a95f094a48b")]
        PosFullAvcsIsoSha1Batch = 2,

        [Description("483aa1b9-8a3b-49f2-bae9-759bb93b04d1")]
        PosFullAvcsZipBatch = 3,

        [Description("90fcdfa0-8229-43d5-b059-172491e5402b")]
        EssUpdateZipBatch = 4,

        [Description("d3871589-c0ea-4967-8537-63089e2398af")]
        PosUpdateBatch = 5,

        [Description("bece0a26-867c-4ea6-8ece-98afa246a00e")]
        PosCatalogueBatch = 6,

        [Description("472b599d-494d-4ac5-a281-91e3927b24d4")]
        PosEncUpdateBatch = 7,

        [Description("a1c65857-1e51-4b91-b74e-66b9bebaf4e8")]
        InvalidProductIdentifier = 8,

        [Description("d2b46c4f-415a-4ca3-b844-6577254f39e5")]
        FullAvcsPollingTimeOut = 9,

        [Description("38ebeb5c-2faa-47f5-b3ac-b196d411fa75")]
        UpdatePollingTimeout = 10


    }
}

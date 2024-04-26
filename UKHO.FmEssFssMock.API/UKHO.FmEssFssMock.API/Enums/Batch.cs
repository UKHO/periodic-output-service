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
        UpdatePollingTimeout = 10,

        [Description("eb74b68e-95a7-4d3b-a162-1444efe43257")]
        AioBaseCDZipIsoSha1Batch = 11,

        [Description("15b38135-aa92-4d1a-9b6b-846462a18362")]
        AioUpdateZipBatch = 12,

        [Description("7d302ad3-97c3-4108-bc8a-b19fc4920079")]
        EssAioBaseZipBatch = 13,

        [Description("094cda16-fcc4-41cb-9317-bf4f26991e32")]
        EssAioUpdateZipBatch = 14,

        [Description("4bc70797-7ee6-407f-bafe-cae49a5b5f91")]
        EssProductIdentifiersS63ZipBatch = 15,

        [Description("f8fd2fb4-3dd6-425d-b34f-3059e262feed")]
        EssProductIdentifiersS57ZipBatch = 16,

        [Description("0f13a253-db5d-4b77-a165-643f4b4a77fc")]
        EssPostProductVersionS63ZipBatch = 17,

        [Description("7b6edd6a-7a62-4271-a657-753f4c648531")]
        EssPostProductVersionS57ZipBatch = 18,

        [Description("5cf9e1d7-207c-4c96-b5e7-5a519f0ea0c0")]
        EssZipBatch = 19,

        [Description("0d91fb1a-cbe2-4443-8f61-e9a925fa00c9")]
        BesBaseZipBatch = 20,

        [Description("27067a02-df4b-49a1-8699-442b265a75d2")]
        BesUpdateZipBatch = 21
    }
}

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IMacroTransformer
    {
        string ExpandMacros(string value);
    }
}

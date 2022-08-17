using System.ComponentModel;
using System.Reflection;

namespace UKHO.FmEssFssMock.API.Helpers
{
    public static class EnumHelper
    {
        public static string GetEnumDescription(Enum enumVal)
        {
            System.Reflection.MemberInfo[] memInfo = enumVal.GetType().GetMember(enumVal.ToString());
            DescriptionAttribute attribute = CustomAttributeExtensions.GetCustomAttribute<DescriptionAttribute>(memInfo[0]);
            return attribute.Description;
        }
    }
}

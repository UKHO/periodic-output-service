using System.ComponentModel;
using System.Reflection;

namespace UKHO.FmEssFssMock.API.Helpers
{
    public static class EnumHelper
    {
        public static string GetEnumDescription(Enum enumVal)
        {
            MemberInfo[] enumInfo = enumVal.GetType().GetMember(enumVal.ToString());
            DescriptionAttribute attribute = CustomAttributeExtensions.GetCustomAttribute<DescriptionAttribute>(enumInfo[0]);
            return attribute.Description;
        }
    }
}

using System.ComponentModel;
using System.Reflection;
using UKHO.FmEssFssMock.Enums;

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

        public static T GetValueFromDescription<T>(string description) where T : Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Not found.", nameof(description));
        }
    }
}

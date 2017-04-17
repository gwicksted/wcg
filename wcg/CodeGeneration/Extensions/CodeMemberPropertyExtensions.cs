using System.CodeDom;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeMemberPropertyExtensions
    {
        /// <summary>
        /// Property which has a special meaning in SOAP communications indicating that a value type was/not specified.
        /// </summary>
        /// <returns>
        /// True if this property does not exist within the SOAP body but is rather used to indicate if a value was specified or not.
        /// False if this property is present within the SOAP body or has some use other than indicating if a value type was provided.
        /// </returns>
        public static bool IsSpecifiedProperty(this CodeMemberProperty property)
        {
            var name = property.Name;
            
            var isBoolean = property.Type.IsBoolean();
            var ignore = property.CustomAttributes.FirstOrDefault(c => c.Name == "System.Xml.Serialization.XmlIgnoreAttribute");

            return name.EndsWith("Specified") && ignore != null && isBoolean;
        }

        /// <summary>
        /// Converts the special property name which is composed of the original property name followed by the word
        /// "Specified" back to the original property name by removing the "Specified" postfix.
        /// </summary>
        /// <param name="property">The property which passes the <see cref="IsSpecifiedProperty"/> test.</param>
        /// <returns>
        /// The real property name which is used in SOAP communications or null if <paramref name="property"/> is not a "Specified" property.
        /// </returns>
        public static string SpecifiedPropertyName(this CodeMemberProperty property)
        {
            return property.IsSpecifiedProperty() ? property.Name.Substring(0, property.Name.Length - 9) : null;
        }
    }
}

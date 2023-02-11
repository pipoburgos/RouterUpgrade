using System.Linq;

namespace RouterUpgrade
{
    public static class MetodoMessUserPass
    {
        public static string MessUserPass(string str)
        {
            return string.Concat(str.Select(c => (char)(c ^ 0x1f)));
        }
    }
}

namespace Game
{
    /// <summary>Names for the Jinhai district (Consortium Tianlong).</summary>
    public static class NameGenerator
    {
        static readonly string[] Family = { "Wang", "Li", "Zhang", "Liu", "Chen", "Yang", "Huang", "Zhao", "Wu", "Zhou", "Xu", "Sun", "Ma", "Hu", "Guo", "Lin", "He", "Gao", "Luo", "Zheng" };
        static readonly string[] Female = { "Mei", "Xiu", "Fang", "Ling", "Yan", "Jing", "Hui", "Lan", "Qing", "Xia", "Yun", "Ying", "Lihua", "Ning" };
        static readonly string[] Male = { "Wei", "Jun", "Hao", "Lei", "Bo", "Tao", "Peng", "Jian", "Ming", "Qiang", "Gang", "Long", "Kai", "Yong" };

        public static string Make(System.Random rnd, bool female)
        {
            var given = female ? Female[rnd.Next(Female.Length)] : Male[rnd.Next(Male.Length)];
            return Family[rnd.Next(Family.Length)] + " " + given;
        }
    }
}

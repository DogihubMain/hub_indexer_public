﻿namespace DogiHubIndexer.Helpers
{
    public class HexConverter
    {
        public static string HexToString(string hex)
        {
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return System.Text.Encoding.Default.GetString(raw);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows.Data;

namespace DNT.Database
{
    class CommandConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string temp = (value as string).Replace(" ", "");
            if (string.IsNullOrEmpty(temp))
                return null;

            if (temp.Length % 2 != 0)
            {
                temp = "0" + temp;
            }

            List<byte> result = new List<byte>();
            for (int i = 0; i < temp.Length; i += 2)
            {
                string tmp = temp.Substring(i, 2);
                byte b = System.Convert.ToByte(tmp, 16);
                result.Add(b);
            }
            return result.ToArray();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte[] command = value as byte[];
            if (command == null)
                return null;

            StringBuilder result = new StringBuilder();
            foreach (byte b in command)
            {
                result.Append(b.ToString("X2"));
                result.Append(" ");
            }
            return result.ToString();
        }
    }
}

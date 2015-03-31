using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class StylePalette : IStylePalette
    {
        public StylePalette(ConsoleDescription Description)
        {
            this.registeredStyles = new Dictionary<string, Style>();
            this.ForegroundColor = Description.ForegroundColor;
            this.BackgroundColor = Description.BackgroundColor;
        }
        public StylePalette(Color ForegroundColor, Color BackgroundColor)
        {
            this.registeredStyles = new Dictionary<string, Style>();
            this.ForegroundColor = ForegroundColor;
            this.BackgroundColor = BackgroundColor;
        }

        public Color ForegroundColor { get; private set; }
        public Color BackgroundColor { get; private set; }

        public double BackgroundLuminance
        {
            get
            {
                return RGBtoHSL(BackgroundColor).Item3;
            }
        }

        public Color ContrastForegroundColor
        {
            get
            {
                return MakeBrightColor(ForegroundColor);
            }
        }

        public Color DimForegroundColor
        {
            get
            {
                return MakeDimColor(BackgroundColor);
            }
        }

        public Color ChangeLuminance(Color Value, double Luminance)
        {
            var hsl = RGBtoHSL(Value);
            var result = HSLtoRGB(hsl.Item1, hsl.Item2, Luminance);
            return result;
        }

        public Color MakeBrightColor(Color Value)
        {
            return ChangeLuminance(Value, 0.5 + (0.5 - BackgroundLuminance) / 3.0);
        }

        public Color MakeDimColor(Color Value)
        {
            return ChangeLuminance(Value, 0.5 + (BackgroundLuminance - 0.5) / 3.0);
        }

        public Color MakeBackgroundColor(Color Value)
        {
            return ChangeLuminance(Value, BackgroundLuminance);
        }

        private Dictionary<string, Style> registeredStyles;
        public void RegisterStyle(Style Value)
        {
            registeredStyles[Value.Name] = Value;
        }

        public Style GetNamedStyle(string Name)
        {
            return registeredStyles[Name];
        }

        public bool IsNamedStyle(string Name)
        {
            return registeredStyles.ContainsKey(Name);
        }

        #region HSL

        // HSL/RGB conversions are a modified version of the code associated with this article:
        // http://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part
        // 
        // Licensed from Guillaume Leparmentier under The Code Project Open License (CPOL)

        private static Color HSLtoRGB(double h, double s, double l)
        {
            if (s == 0)
            {
                // achromatic color (gray scale)
                return new Color(l, l, l);
            }
            else
            {
                double q = (l < 0.5) ? (l * (1.0 + s)) : (l + s - (l * s));
                double p = (2.0 * l) - q;

                double Hk = h / 360.0;
                double[] T = new double[3];
                T[0] = Hk + (1.0 / 3.0);  // Tr
                T[1] = Hk;              // Tb
                T[2] = Hk - (1.0 / 3.0);  // Tg

                for (int i = 0; i < 3; i++)
                {
                    if (T[i] < 0) T[i] += 1.0;
                    if (T[i] > 1) T[i] -= 1.0;

                    if ((T[i] * 6) < 1)
                    {
                        T[i] = p + ((q - p) * 6.0 * T[i]);
                    }
                    else if ((T[i] * 2.0) < 1) //(1.0/6.0)<=T[i] && T[i]<0.5
                    {
                        T[i] = q;
                    }
                    else if ((T[i] * 3.0) < 2) // 0.5<=T[i] && T[i]<(2.0/3.0)
                    {
                        T[i] = p + (q - p) * ((2.0 / 3.0) - T[i]) * 6.0;
                    }
                    else T[i] = p;
                }

                return new Color(T[0], T[1], T[2]);
            }
        }

        private static Tuple<double, double, double> RGBtoHSL(Color Color)
        {
            double h = 0, s = 0, l = 0;

            // normalizes red-green-blue values
            double nRed = Color.Red;
            double nGreen = Color.Green;
            double nBlue = Color.Blue;

            double max = Math.Max(nRed, Math.Max(nGreen, nBlue));
            double min = Math.Min(nRed, Math.Min(nGreen, nBlue));

            // hue
            if (max == min)
            {
                h = 0; // undefined
            }
            else if (max == nRed && nGreen >= nBlue)
            {
                h = 60.0 * (nGreen - nBlue) / (max - min);
            }
            else if (max == nRed && nGreen < nBlue)
            {
                h = 60.0 * (nGreen - nBlue) / (max - min) + 360.0;
            }
            else if (max == nGreen)
            {
                h = 60.0 * (nBlue - nRed) / (max - min) + 120.0;
            }
            else if (max == nBlue)
            {
                h = 60.0 * (nRed - nGreen) / (max - min) + 240.0;
            }

            // luminance
            l = (max + min) / 2.0;

            // saturation
            if (l == 0 || max == min)
            {
                s = 0;
            }
            else if (0 < l && l <= 0.5)
            {
                s = (max - min) / (max + min);
            }
            else if (l > 0.5)
            {
                s = (max - min) / (2 - (max + min)); //(max-min > 0)?
            }

            return new Tuple<double, double, double>(h, s, l);
        }


        #endregion
    }
}

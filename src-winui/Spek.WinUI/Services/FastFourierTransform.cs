namespace Spek.WinUI.Services;

internal static class FastFourierTransform
{
    public static void Transform(double[] real, double[] imaginary)
    {
        int n = real.Length;
        if (n == 0 || (n & (n - 1)) != 0)
        {
            throw new ArgumentException("FFT input length must be a power of two.", nameof(real));
        }

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
            {
                j ^= bit;
            }

            j ^= bit;

            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }
        }

        for (int length = 2; length <= n; length <<= 1)
        {
            double angle = -2.0 * Math.PI / length;
            double wLengthReal = Math.Cos(angle);
            double wLengthImaginary = Math.Sin(angle);

            for (int i = 0; i < n; i += length)
            {
                double wReal = 1.0;
                double wImaginary = 0.0;

                for (int j = 0; j < length / 2; j++)
                {
                    int even = i + j;
                    int odd = even + length / 2;

                    double oddReal = real[odd] * wReal - imaginary[odd] * wImaginary;
                    double oddImaginary = real[odd] * wImaginary + imaginary[odd] * wReal;

                    real[odd] = real[even] - oddReal;
                    imaginary[odd] = imaginary[even] - oddImaginary;
                    real[even] += oddReal;
                    imaginary[even] += oddImaginary;

                    double nextReal = wReal * wLengthReal - wImaginary * wLengthImaginary;
                    wImaginary = wReal * wLengthImaginary + wImaginary * wLengthReal;
                    wReal = nextReal;
                }
            }
        }
    }
}

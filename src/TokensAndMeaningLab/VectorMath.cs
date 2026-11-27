using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TokensAndMeaningLab
{
    public static class VectorMath
    {
        public static double CosineSimilarity(float[] left, float[] right)
        {
            if (left.Length != right.Length)
            {
                throw new ArgumentException("The vectors must have the same number of dimensions.");
            }

            double dotProduct = 0;
            double leftLengthSquared = 0;
            double rightLengthSquared = 0;

            for (int i = 0; i < left.Length; i++)
            {
                double x = left[i];
                double y = right[i];

                dotProduct += x * y;
                leftLengthSquared += x * x;
                rightLengthSquared += y * y;
            }

            if (leftLengthSquared == 0 || rightLengthSquared == 0)
            {
                throw new ArgumentException("Cosine similarity requires nonzero vectors.");
            }

            return dotProduct /
                (Math.Sqrt(leftLengthSquared) * Math.Sqrt(rightLengthSquared));
        }
    }
}
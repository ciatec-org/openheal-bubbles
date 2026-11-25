using UnityEngine;

namespace Core.Tracking
{
    public class OneEuroFilter
    {
        private float freq;
        private float minCutoff;
        private float beta;
        private float dCutoff;
        private float lastValue;
        private float lastDerivative;
        private bool initialized = false;

        public OneEuroFilter(float freq = 15f, float minCutoff = 60f, float beta = 15f, float dCutoff = 15f)
        {
            this.freq = freq;
            this.minCutoff = minCutoff;
            this.beta = beta;
            this.dCutoff = dCutoff;
        }

        private float Alpha(float cutoff)
        {
            float te = 1f / freq;
            float tau = 1f / (2 * Mathf.PI * cutoff);
            return 1f / (1f + tau / te);
        }

        public float Filter(float value)
        {
            if (!initialized)
            {
                lastValue = value;
                lastDerivative = 0;
                initialized = true;
                return value;
            }

            float derivative = (value - lastValue) * freq;
            float alphaD = Alpha(dCutoff);
            lastDerivative = Mathf.Lerp(lastDerivative, derivative, alphaD);

            float cutoff = minCutoff + beta * Mathf.Abs(lastDerivative);
            float alpha = Alpha(cutoff);

            lastValue = Mathf.Lerp(lastValue, value, alpha);
            return lastValue;
        }
    }
}

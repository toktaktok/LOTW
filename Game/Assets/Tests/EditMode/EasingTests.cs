using NUnit.Framework;
using Project.Scripts.Framework;

namespace Tests.EditMode
{
    public class EasingTests
    {
        private const float Tol = 1e-5f;

        private static readonly EaseType[] AllTypes =
        {
            EaseType.Linear,
            EaseType.SmoothStep,
            EaseType.QuadIn,
            EaseType.QuadOut,
            EaseType.QuadInOut,
            EaseType.SineIn,
            EaseType.SineOut,
            EaseType.SineInOut
        };

        // --- Endpoint guarantees: every curve must pass through (0,0) and (1,1) ---

        [Test]
        public void Evaluate_AtZero_IsZero_ForAllTypes()
        {
            foreach(var type in AllTypes)
                Assert.AreEqual(0f, Easing.Evaluate(0f, type), Tol, type.ToString());
        }

        [Test]
        public void Evaluate_AtOne_IsOne_ForAllTypes()
        {
            foreach(var type in AllTypes)
                Assert.AreEqual(1f, Easing.Evaluate(1f, type), Tol, type.ToString());
        }

        // --- Monotonicity: all provided curves are non-decreasing on [0,1] ---

        [Test]
        public void Evaluate_IsMonotonicNonDecreasing_ForAllTypes()
        {
            const int steps = 100;
            foreach(var type in AllTypes)
            {
                float prev = Easing.Evaluate(0f, type);
                for(int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    float cur = Easing.Evaluate(t, type);
                    Assert.GreaterOrEqual(cur + Tol, prev, $"{type} decreased at t={t}");
                    prev = cur;
                }
            }
        }

        // --- Output range stays within [0,1] for in-range input ---

        [Test]
        public void Evaluate_StaysWithinUnitRange_ForAllTypes()
        {
            const int steps = 50;
            foreach(var type in AllTypes)
            {
                for(int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    float v = Easing.Evaluate(t, type);
                    Assert.GreaterOrEqual(v, -Tol, $"{type} below 0 at t={t}");
                    Assert.LessOrEqual(v, 1f + Tol, $"{type} above 1 at t={t}");
                }
            }
        }

        // --- Clamping behavior on out-of-range input ---

        [Test]
        public void Evaluate_ClampsBelowZero_ForAllTypes()
        {
            foreach(var type in AllTypes)
                Assert.AreEqual(0f, Easing.Evaluate(-0.5f, type), Tol, type.ToString());
        }

        [Test]
        public void Evaluate_ClampsAboveOne_ForAllTypes()
        {
            foreach(var type in AllTypes)
                Assert.AreEqual(1f, Easing.Evaluate(1.5f, type), Tol, type.ToString());
        }

        // --- Specific curve values ---

        [Test]
        public void Linear_Midpoint_IsHalf()
        {
            Assert.AreEqual(0.5f, Easing.Evaluate(0.5f, EaseType.Linear), Tol);
        }

        [Test]
        public void SmoothStep_Midpoint_IsHalf()
        {
            // 3(.5)^2 - 2(.5)^3 = .75 - .25 = .5
            Assert.AreEqual(0.5f, Easing.SmoothStep(0.5f), Tol);
        }

        [Test]
        public void SmoothStep_IsSymmetricAboutMidpoint()
        {
            // f(t) + f(1-t) == 1 for this curve
            for(int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                Assert.AreEqual(1f, Easing.SmoothStep(t) + Easing.SmoothStep(1f - t), Tol, $"t={t}");
            }
        }

        [Test]
        public void SmoothStep_InteriorValue_IsCubicNotLinear()
        {
            // 3(.25)^2 - 2(.25)^3 = .1875 - .03125 = .15625, distinct from Linear's .25
            Assert.AreEqual(0.15625f, Easing.SmoothStep(0.25f), Tol);
            Assert.AreEqual(0.15625f, Easing.Evaluate(0.25f, EaseType.SmoothStep), Tol);
        }

        [Test]
        public void QuadIn_Midpoint_IsQuarter()
        {
            Assert.AreEqual(0.25f, Easing.QuadIn(0.5f), Tol);
        }

        [Test]
        public void QuadOut_Midpoint_IsThreeQuarters()
        {
            Assert.AreEqual(0.75f, Easing.QuadOut(0.5f), Tol);
        }

        [Test]
        public void QuadInOut_Midpoint_IsHalf()
        {
            Assert.AreEqual(0.5f, Easing.QuadInOut(0.5f), Tol);
        }

        [Test]
        public void SineInOut_Midpoint_IsHalf()
        {
            Assert.AreEqual(0.5f, Easing.SineInOut(0.5f), Tol);
        }

        [Test]
        public void SineInOut_InteriorValue_IsSineNotLinear()
        {
            // -(cos(pi*.25)-1)/2 = (1 - cos(pi/4))/2 ~= 0.1464466, distinct from Linear's .25
            float expected = -((float)System.Math.Cos(System.Math.PI * 0.25) - 1f) * 0.5f;
            Assert.AreEqual(expected, Easing.SineInOut(0.25f), Tol);
            Assert.AreEqual(expected, Easing.Evaluate(0.25f, EaseType.SineInOut), Tol);
            Assert.Less(Easing.SineInOut(0.25f), 0.25f, "SineInOut(0.25) must be below Linear");
        }

        // --- Lerp helpers ---

        [Test]
        public void Lerp_Linear_InterpolatesEndpoints()
        {
            Assert.AreEqual(10f, Easing.Lerp(10f, 20f, 0f), Tol);
            Assert.AreEqual(20f, Easing.Lerp(10f, 20f, 1f), Tol);
            Assert.AreEqual(15f, Easing.Lerp(10f, 20f, 0.5f), Tol);
        }

        [Test]
        public void Lerp_ClampsT()
        {
            Assert.AreEqual(10f, Easing.Lerp(10f, 20f, -1f), Tol);
            Assert.AreEqual(20f, Easing.Lerp(10f, 20f, 2f), Tol);
        }

        [Test]
        public void Lerp_WithEaseType_AppliesCurve()
        {
            // QuadIn at t=0.5 -> 0.25, so 0 + (100-0)*0.25 = 25
            Assert.AreEqual(25f, Easing.Lerp(0f, 100f, 0.5f, EaseType.QuadIn), Tol);
        }

        [Test]
        public void Lerp_WithEaseType_PreservesEndpoints()
        {
            foreach(var type in AllTypes)
            {
                Assert.AreEqual(5f, Easing.Lerp(5f, 9f, 0f, type), Tol, type.ToString());
                Assert.AreEqual(9f, Easing.Lerp(5f, 9f, 1f, type), Tol, type.ToString());
            }
        }

        [Test]
        public void Clamp01_ClampsBothEnds()
        {
            Assert.AreEqual(0f, Easing.Clamp01(-3f), Tol);
            Assert.AreEqual(1f, Easing.Clamp01(3f), Tol);
            Assert.AreEqual(0.42f, Easing.Clamp01(0.42f), Tol);
        }
    }
}

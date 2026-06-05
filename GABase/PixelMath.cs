using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace GABase
{
    public struct Pixel
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;
    }

    /// <summary>
    /// Backend-agnostic pixel math shared by the GDI+ and Skia rendering paths so the
    /// fitness comparison is bit-identical regardless of which rasterizer produced the
    /// candidate pixels. All routines operate on raw BGRA32 buffers via Scan0/Stride.
    /// </summary>
    public static class PixelMath
    {
        /// <summary>
        /// Compute the candidate partial fitness live against the reference snapshot, and
        /// the current-best partial fitness from the cached per-pixel error buffer, over
        /// the region [minX,minX+width) x [minY,minY+height).
        /// </summary>
        public static unsafe void ComputeBothPartial(
            IntPtr candScan0, int candStride,
            Pixel* pRefBase, int refWidth, int* pErrBase,
            int minX, int minY, int width, int height,
            byte[] focusWeightMap, int screenWidth, bool hasFocusAreas,
            out long fitnessCand, out long fitnessCurr)
        {
            fitnessCand = 0;
            fitnessCurr = 0;

            unchecked
            {
                if (!hasFocusAreas)
                {
                    // Fast path: no focus weight lookup needed (all weights = 1)
                    // Per 4 bytes (B,G,R,A) keep B,G,R and zero the alpha difference.
                    Vector256<byte> alphaMask = Vector256.Create(
                        (byte)255, 255, 255, 0, 255, 255, 255, 0, 255, 255, 255, 0, 255, 255, 255, 0,
                        255, 255, 255, 0, 255, 255, 255, 0, 255, 255, 255, 0, 255, 255, 255, 0);
                    int* lane = stackalloc int[8];

                    for (int y = 0; y < height; y++)
                    {
                        Pixel* pCand = (Pixel*)((byte*)candScan0 + (long)y * candStride);
                        Pixel* pRef = pRefBase + (minY + y) * refWidth + minX;
                        int* pErr = pErrBase + (minY + y) * refWidth + minX;
                        int x = 0;

                        if (Avx2.IsSupported)
                        {
                            // Accumulators reset per row so the int32 lanes cannot overflow.
                            Vector256<int> candAcc = Vector256<int>.Zero;
                            Vector256<int> currAcc = Vector256<int>.Zero;

                            for (; x + 8 <= width; x += 8)
                            {
                                Vector256<byte> cand = Avx.LoadVector256((byte*)pCand);
                                Vector256<byte> rf = Avx.LoadVector256((byte*)pRef);

                                // |cand - ref| per byte via two saturating subtracts.
                                Vector256<byte> d = Avx2.Or(
                                    Avx2.SubtractSaturate(cand, rf),
                                    Avx2.SubtractSaturate(rf, cand));
                                d = Avx2.And(d, alphaMask);

                                // Widen bytes to 16-bit, square and pair-sum into 32-bit lanes.
                                Vector256<short> dLo = Avx2.ConvertToVector256Int16(d.GetLower());
                                Vector256<short> dHi = Avx2.ConvertToVector256Int16(d.GetUpper());
                                candAcc = Avx2.Add(candAcc, Avx2.MultiplyAddAdjacent(dLo, dLo));
                                candAcc = Avx2.Add(candAcc, Avx2.MultiplyAddAdjacent(dHi, dHi));

                                currAcc = Avx2.Add(currAcc, Avx.LoadVector256(pErr));

                                pCand += 8;
                                pRef += 8;
                                pErr += 8;
                            }

                            Avx.Store(lane, candAcc);
                            fitnessCand += lane[0] + lane[1] + lane[2] + lane[3]
                                         + lane[4] + lane[5] + lane[6] + lane[7];
                            Avx.Store(lane, currAcc);
                            fitnessCurr += lane[0] + lane[1] + lane[2] + lane[3]
                                         + lane[4] + lane[5] + lane[6] + lane[7];
                        }

                        // Scalar remainder (and full row when AVX2 is unavailable).
                        for (; x < width; x++)
                        {
                            int rc = pCand->R - pRef->R;
                            int gc = pCand->G - pRef->G;
                            int bc = pCand->B - pRef->B;
                            fitnessCand += rc * rc + gc * gc + bc * bc;

                            fitnessCurr += *pErr;

                            pCand++;
                            pRef++;
                            pErr++;
                        }
                    }
                }
                else
                {
                    // Scalar fallback (with focus weight support)
                    for (int y = 0; y < height; y++)
                    {
                        Pixel* pCand = (Pixel*)((byte*)candScan0 + (long)y * candStride);
                        int mapIndex = (minY + y) * screenWidth + minX;
                        Pixel* pRef = pRefBase + (minY + y) * refWidth + minX;
                        int* pErr = pErrBase + (minY + y) * refWidth + minX;
                        for (int x = 0; x < width; x++)
                        {
                            int weight = focusWeightMap[mapIndex];

                            int rc = pCand->R - pRef->R;
                            int gc = pCand->G - pRef->G;
                            int bc = pCand->B - pRef->B;
                            fitnessCand += (rc * rc + gc * gc + bc * bc) * weight;

                            fitnessCurr += (long)(*pErr) * weight;

                            pCand++;
                            pRef++;
                            pErr++;
                            mapIndex++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recompute the cached per-pixel squared error for the whole current-best buffer.
        /// </summary>
        public static unsafe void RebuildError(
            IntPtr scan0, int stride,
            Pixel* pRefBase, int refWidth, int* pErrBase,
            int width, int height)
        {
            unchecked
            {
                for (int y = 0; y < height; y++)
                {
                    Pixel* pCur = (Pixel*)((byte*)scan0 + (long)y * stride);
                    Pixel* pRef = pRefBase + y * refWidth;
                    int* pErr = pErrBase + y * refWidth;
                    for (int x = 0; x < width; x++)
                    {
                        int r = pCur->R - pRef->R;
                        int g = pCur->G - pRef->G;
                        int b = pCur->B - pRef->B;
                        *pErr = r * r + g * g + b * b;
                        pCur++;
                        pRef++;
                        pErr++;
                    }
                }
            }
        }

        /// <summary>
        /// Copy a region from source to destination buffer and refresh the cached per-pixel
        /// error for that region (the source is the newly accepted best).
        /// </summary>
        public static unsafe void CopyAndUpdateError(
            IntPtr srcScan0, int srcStride,
            IntPtr dstScan0, int dstStride,
            Pixel* pRefBase, int refWidth, int* pErrBase,
            int minX, int minY, int width, int height)
        {
            unchecked
            {
                for (int y = 0; y < height; y++)
                {
                    Pixel* pSrc = (Pixel*)((byte*)srcScan0 + (long)y * srcStride);
                    Pixel* pDst = (Pixel*)((byte*)dstScan0 + (long)y * dstStride);
                    Pixel* pRef = pRefBase + (minY + y) * refWidth + minX;
                    int* pErr = pErrBase + (minY + y) * refWidth + minX;
                    for (int x = 0; x < width; x++)
                    {
                        *pDst = *pSrc;

                        int r = pSrc->R - pRef->R;
                        int g = pSrc->G - pRef->G;
                        int b = pSrc->B - pRef->B;
                        *pErr = r * r + g * g + b * b;

                        pSrc++;
                        pDst++;
                        pRef++;
                        pErr++;
                    }
                }
            }
        }
    }
}

import fs from 'fs';
import path from 'path';

/**
 * Pixel-by-pixel image comparison utility for Figma vs Unity screenshot diffing.
 * Fails if differing pixels exceed maxDiffThresholdPercent (default: 2.0%).
 */
export interface DiffResult {
  passed: boolean;
  differingPixels: number;
  totalPixels: number;
  diffPercentage: number;
  errorMessage: string;
}

export function comparePixelArrays(
  data1: Uint8Array | Buffer,
  data2: Uint8Array | Buffer,
  thresholdPerChannel: number = 13, // ~5% tolerance on 0-255 scale
  maxDiffThresholdPercent: number = 2.0
): DiffResult {
  const minLength = Math.min(data1.length, data2.length);
  const totalPixels = Math.floor(minLength / 4);

  if (totalPixels === 0) {
    return {
      passed: false,
      differingPixels: 0,
      totalPixels: 0,
      diffPercentage: 100,
      errorMessage: 'Invalid pixel data length (0 pixels).'
    };
  }

  let diffCount = 0;

  for (let i = 0; i < totalPixels; i++) {
    const idx = i * 4;
    const rDiff = Math.abs(data1[idx] - data2[idx]);
    const gDiff = Math.abs(data1[idx + 1] - data2[idx + 1]);
    const bDiff = Math.abs(data1[idx + 2] - data2[idx + 2]);
    const aDiff = Math.abs(data1[idx + 3] - data2[idx + 3]);

    if (rDiff > thresholdPerChannel || gDiff > thresholdPerChannel || bDiff > thresholdPerChannel || aDiff > thresholdPerChannel) {
      diffCount++;
    }
  }

  const diffPercentage = (diffCount / totalPixels) * 100;
  const passed = diffPercentage <= maxDiffThresholdPercent;

  const errorMessage = passed
    ? `Visual regression check PASSED: ${diffPercentage.toFixed(2)}% differing pixels is within ${maxDiffThresholdPercent.toFixed(2)}% threshold.`
    : `Visual regression check FAILED: ${diffPercentage.toFixed(2)}% differing pixels exceeds ${maxDiffThresholdPercent.toFixed(2)}% threshold (${diffCount}/${totalPixels} pixels diff).`;

  return {
    passed,
    differingPixels: diffCount,
    totalPixels,
    diffPercentage,
    errorMessage
  };
}

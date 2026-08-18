import { describe, it, expect } from 'vitest';
import { comparePixelArrays } from './compare-images';

describe('comparePixelArrays', () => {
  it('passes when pixel arrays are identical (0% diff)', () => {
    const data1 = new Uint8Array([255, 0, 0, 255, 0, 255, 0, 255]);
    const data2 = new Uint8Array([255, 0, 0, 255, 0, 255, 0, 255]);

    const result = comparePixelArrays(data1, data2, 13, 2.0);

    expect(result.passed).toBe(true);
    expect(result.differingPixels).toBe(0);
    expect(result.diffPercentage).toBe(0);
  });

  it('fails when differing pixels exceed 2% threshold', () => {
    // 100 pixels (400 bytes). Make 5 pixels differ (5% diff > 2% threshold)
    const data1 = new Uint8Array(400);
    const data2 = new Uint8Array(400);

    for (let i = 0; i < 5; i++) {
      data2[i * 4] = 255; // Red channel difference
    }

    const result = comparePixelArrays(data1, data2, 13, 2.0);

    expect(result.passed).toBe(false);
    expect(result.differingPixels).toBe(5);
    expect(result.diffPercentage).toBe(5);
    expect(result.errorMessage).toContain('FAILED');
  });
});

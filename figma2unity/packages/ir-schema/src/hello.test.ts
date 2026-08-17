import { describe, it, expect } from 'vitest';

describe('IR Schema Package - Hello World Test', () => {
  it('should execute and pass basic assertion', () => {
    const greeting = 'Hello World from @figma2unity/ir-schema';
    expect(greeting).toContain('Hello World');
  });
});

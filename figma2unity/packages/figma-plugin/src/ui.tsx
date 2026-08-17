import { render } from 'preact';
import { useState, useEffect } from 'preact/hooks';
import { ZipPackager } from './zip-packager.js';
import type { ExportedAsset } from './asset-exporter.js';

export interface BreakdownSummary {
  totalNodes: number;
  supportedCount: number;
  rasterCount: number;
  vectorCount: number;
  unsupportedCount: number;
  nodeCounts: Record<string, number>;
}

function App() {
  const [loading, setLoading] = useState(false);
  const [summary, setSummary] = useState<BreakdownSummary | null>(null);
  const [downloadedFile, setDownloadedFile] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    window.onmessage = (event) => {
      const msg = event.data.pluginMessage;
      if (!msg) return;

      if (msg.type === 'EXPORT_DATA') {
        try {
          const assets: ExportedAsset[] = (msg.assets || []).map((a: any) => ({
            path: a.path,
            data: new Uint8Array(a.data),
            mimeType: a.mimeType,
          }));

          // Create .f2u.zip archive inside the UI iframe browser environment
          const zipBytes = ZipPackager.createF2uZip(msg.document, assets);

          // Trigger browser Blob download of the .f2u.zip archive
          const blob = new Blob([zipBytes], { type: 'application/zip' });
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = msg.fileName;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          URL.revokeObjectURL(url);

          setLoading(false);
          setSummary(msg.summary);
          setDownloadedFile(msg.fileName);
        } catch (err: any) {
          setLoading(false);
          setError(`Failed to create zip package: ${err?.message || String(err)}`);
        }
      } else if (msg.type === 'TRAVERSAL_ERROR') {
        setLoading(false);
        setError(msg.error);
      }
    };
  }, []);

  const handleSync = () => {
    setLoading(true);
    setError(null);
    setDownloadedFile(null);
    parent.postMessage({ pluginMessage: { type: 'START_SYNC' } }, '*');
  };

  return (
    <div style={{ fontFamily: 'sans-serif', padding: 16, background: '#1e1e1e', color: '#fff', boxSizing: 'border-box' }}>
      <h2 style={{ margin: '0 0 8px 0', fontSize: 18, color: '#0d99ff' }}>Figma2Unity Importer</h2>
      <p style={{ fontSize: 12, color: '#aaa', margin: '0 0 16px 0' }}>
        Export design selection to <code>.f2u.zip</code> bundle containing IR JSON document and assets.
      </p>

      <button
        id="sync-button"
        onClick={handleSync}
        disabled={loading}
        style={{
          width: '100%',
          padding: '10px 16px',
          background: loading ? '#555' : '#0d99ff',
          color: '#fff',
          border: 'none',
          borderRadius: 6,
          fontWeight: 600,
          cursor: loading ? 'not-allowed' : 'pointer',
        }}
      >
        {loading ? 'Exporting Assets & Packing Zip...' : 'Sync Selection'}
      </button>

      {downloadedFile && (
        <div style={{ marginTop: 12, padding: 8, background: '#1b3823', border: '1px solid #4caf50', borderRadius: 6, fontSize: 12, color: '#81c784' }}>
          ✓ Downloaded <strong>{downloadedFile}</strong>
        </div>
      )}

      {error && (
        <div style={{ marginTop: 12, padding: 10, background: '#3b1c1c', border: '1px solid #ff4d4d', borderRadius: 6, fontSize: 12 }}>
          <strong>Error:</strong> {error}
        </div>
      )}

      {summary && (
        <div style={{ marginTop: 16, padding: 12, background: '#2a2a2a', borderRadius: 6, fontSize: 12 }}>
          <h3 style={{ margin: '0 0 12px 0', fontSize: 14, color: '#0d99ff' }}>Figma Coverage Breakdown</h3>
          
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
            <span>Total Nodes Processed:</span>
            <strong id="total-nodes">{summary.totalNodes}</strong>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, color: '#81c784' }}>
            <span>Native UI Toolkit Nodes:</span>
            <strong id="supported-count">{summary.supportedCount}</strong>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, color: '#ffb74d' }}>
            <span>Rasterized Images (PNG 1x/2x/3x):</span>
            <strong id="raster-count">{summary.rasterCount}</strong>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, color: '#64b5f6' }}>
            <span>Vector Assets (SVG):</span>
            <strong id="vector-count">{summary.vectorCount}</strong>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, color: summary.unsupportedCount > 0 ? '#e57373' : '#aaa' }}>
            <span>Unsupported Nodes:</span>
            <strong id="unsupported-count">{summary.unsupportedCount}</strong>
          </div>

          <h4 style={{ margin: '14px 0 6px 0', fontSize: 12, color: '#ccc' }}>Layer Type Summary:</h4>
          <ul style={{ margin: 0, paddingLeft: 16, color: '#bbb' }}>
            {Object.entries(summary.nodeCounts).map(([type, count]) => (
              <li key={type}>
                {type}: {count}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

const root = document.getElementById('app');
if (root) {
  render(<App />, root);
}

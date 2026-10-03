"""Optional Python plot helper: synthetic textbook data, no devices or networking.
Requires matplotlib; figures are stand-alone SVGs, mathematical data saved as JSON.
Run: python dsp-plot-diagrams.py OUTPUT_DIRECTORY
"""
import cmath
import json
import math
import sys
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path('dsp-plot-output')
out.mkdir(parents=True, exist_ok=True)
plt.rcParams.update({'svg.fonttype': 'none', 'font.size': 12, 'svg.hashsalt': 'ics-dsp-foundations-v2'})

x = [1.0, 0.0, -1.0, 0.0]
n_data = len(x)
fs = 4.0
bins = [sum(v * cmath.exp(-2j * math.pi * k * n / n_data) for n, v in enumerate(x)) for k in range(n_data)]
assert abs(bins[1] - 2) < 1e-10 and abs(bins[3] - 2) < 1e-10
fig, axes = plt.subplots(1, 2, figsize=(11, 3.8), constrained_layout=True)
axes[0].stem([n/fs for n in range(n_data)], x, basefmt='k-', linefmt='C0-', markerfmt='C0o')
axes[0].set(xlabel='Sample time (s); fs = 4 samples/s', ylabel='Normalized amplitude (unitless)', title='Four acquired samples', ylim=(-1.3, 1.3), xticks=[0, .25, .5, .75])
axes[1].stem(range(n_data), [abs(v) for v in bins], basefmt='k-', linefmt='C1-', markerfmt='C1o')
axes[1].set(xlabel='DFT bin k (not a time index)', ylabel='Unnormalized |X[k]|', title='Forward DFT: no window, no 1/N', ylim=(-.2, 2.5), xticks=range(n_data))
for k, label in enumerate(['0 Hz', '+1 Hz', 'Nyquist: 2 Hz', '-1 Hz']):
    axes[1].annotate(label, (k, abs(bins[k])), xytext=(0, 12), textcoords='offset points', ha='center', fontsize=10)
for ax in axes: ax.grid(alpha=.25)
fig.savefig(out/'dsp-four-point-dft.svg', metadata={'Date': None}); plt.close(fig)

fs_alias = 8000.0
samples = [n/fs_alias for n in range(8)]
values_low = [math.cos(2*math.pi*1000*t) for t in samples]
values_high = [math.cos(2*math.pi*7000*t) for t in samples]
assert max(abs(a-b) for a,b in zip(values_low,values_high)) < 1e-10
curve_t = [i/1_000_000 for i in range(1001)]
fig, ax = plt.subplots(figsize=(11, 3.8), constrained_layout=True)
ax.plot([t*1000 for t in curve_t], [math.cos(2*math.pi*1000*t) for t in curve_t], label='Known synthetic cos: 1000 Hz', linewidth=2)
ax.plot([t*1000 for t in curve_t], [math.cos(2*math.pi*7000*t) for t in curve_t], label='Known synthetic cos: 7000 Hz', linewidth=1.2, linestyle='--')
ax.scatter([t*1000 for t in samples],values_low,color='black',s=32,zorder=5,label='Same 8 samples for both at fs = 8000')
ax.set(xlabel='Synthetic continuous time (ms)', ylabel='Normalized amplitude (unitless)', title='Aliasing: different curves, identical acquired samples', xlim=(0,1), ylim=(-1.25,1.4))
ax.grid(alpha=.25); ax.legend(loc='lower center', bbox_to_anchor=(.5, 1.01), fontsize=8, ncol=3); ax.set_title('Aliasing: different curves, identical acquired samples', pad=34)
fig.savefig(out/'dsp-aliasing-samples.svg', metadata={'Date': None});plt.close(fig)

# Keep generated assets compatible with repository whitespace checks.
for svg in out.glob('dsp-*.svg'):
    svg.write_text('\n'.join(line.rstrip() for line in svg.read_text().splitlines()) + '\n')

payload = {'scope':'synthetic mathematical examples; not RF/ADC measurements',
 'dft':{'fsSamplesPerSecond':fs,'samples':x,'sampleTimesSeconds':[n/fs for n in range(n_data)],'nData':4,'nFft':4,'window':'rectangular','forwardScale':'unscaled','bins':[{'k':k,'real':v.real,'imaginary':v.imag,'magnitude':abs(v)} for k,v in enumerate(bins)]},
 'aliasing':{'fsSamplesPerSecond':fs_alias,'waveforms':'amplitude 1, phase 0 cosine at 1000 Hz and 7000 Hz','sampleTimesSeconds':samples,'samples1000Hz':values_low,'samples7000Hz':values_high,'continuousCurveFormula':'cos(2*pi*f*t), t=0..0.001 seconds; not acquired points'}}
(out/'dsp-plot-data.json').write_text(json.dumps(payload,indent=2)+'\n')
print('PASS synthetic DFT and identical aliasing samples; wrote 2 SVGs and JSON')

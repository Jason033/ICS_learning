#!/usr/bin/env python3
"""Compile and run original teaching projects in temporary copies, never docs/bin.

Requires .NET 10 SDK. No external NuGet packages. The models' assertions provide
the expectations; compilation alone is not the validation. --report saves evidence.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
LABS = ROOT / "docs/assets/labs"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--report", type=Path)
    parser.add_argument("topics", nargs="*")
    args = parser.parse_args()
    sdk = subprocess.check_output([args.dotnet, "--version"], text=True).strip()
    if not sdk.startswith("10."):
        raise SystemExit("請使用 .NET 10 SDK 執行這組獨立教學模型。")
    configurations = {
        "networking": [["flow"], ["flow", "--fault"], ["endpoints"], ["ethernet"], ["ethernet", "--fault"],
                       ["routes", "10.24.6.77"], ["routes", "10.24.6.130"], ["routes", "8.8.8.8"], ["tcp"], ["services"],
                       ["multicast"], ["multicast", "--fault"], ["mtu", "1500"], ["mtu", "1400"],
                       ["metrics"], ["diagnose"], ["diagnose", "--fault"]],
        "serial": [[mode] for mode in ["layers", "wiring", "uart", "frames", "timeout", "events", "interfaces", "diagnose"]]
                  + [[mode, "--fault"] for mode in ["layers", "frames", "diagnose"]],
        "radio": [[mode] for mode in ["path", "parameters", "modulation", "budget", "quality", "duplex", "measurements", "diagnose"]]
                 + [[mode, "--fault"] for mode in ["path", "diagnose"]],
        "sockets": [[]],
    }
    results = []
    with tempfile.TemporaryDirectory(prefix="ics-csharp-check-") as temp:
        temp = Path(temp)
        env = dict(os.environ, DOTNET_CLI_HOME=str(temp / "dotnet-home"), DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
                   DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1", DOTNET_CLI_UI_LANGUAGE="en-US")
        for folder in sorted(LABS.glob("*-maintenance-lab")):
            topic = folder.name.removesuffix("-maintenance-lab")
            if args.topics and topic not in args.topics:
                continue
            target = temp / folder.name
            shutil.copytree(folder, target, ignore=shutil.ignore_patterns("bin", "obj", "__pycache__"))
            # Every project is deliberately standard-library-only; disallow feed access.
            (target / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
            build = subprocess.run([args.dotnet, "build", "--nologo", "-v:minimal"], cwd=target, env=env,
                                   capture_output=True, text=True, timeout=120)
            if build.returncode:
                raise SystemExit(f"{topic} build failed:\n{build.stdout}\n{build.stderr}")
            if "0 Warning(s)" not in build.stdout or "0 Error(s)" not in build.stdout:
                raise SystemExit(f"{topic} build has warnings or an unexpected summary:\n{build.stdout}")
            commands = configurations.get(topic, [["all"]])
            if topic == "voip":
                commands = [[str(temp / "audio-output")]]
            runs = []
            for command in commands:
                run = subprocess.run([args.dotnet, "run", "--no-build", "--", *command], cwd=target, env=env,
                                     capture_output=True, text=True, timeout=45)
                if run.returncode:
                    raise SystemExit(f"{topic} {command} failed:\n{run.stdout}\n{run.stderr}")
                expected_matched = None
                if topic in {"systems", "satellite", "dsp", "security", "troubleshooting", "big-picture"} and command == ["all"]:
                    expected = (folder / "expected-output.txt").read_text(encoding="utf-8")
                    expected_matched = run.stdout.splitlines() == expected.splitlines()
                    if not expected_matched:
                        raise SystemExit(f"{topic}: all output differs from the published expected-output.txt")
                runs.append({"arguments": ["<temporary-output>"] if topic == "voip" else command,
                             "exit_code": run.returncode, "stdout": run.stdout.replace(str(temp), "<temporary>"),
                             "stderr": run.stderr.replace(str(temp), "<temporary>"),
                             "published_expected_output_matched": expected_matched})
            result = {"topic": topic, "sdk": sdk, "build_exit_code": build.returncode,
                      "build_output": build.stdout.replace(str(temp), "<temporary>"),
                      "source_sha256": {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
                                        for p in sorted(folder.glob("*.cs"))}, "runs": runs}
            results.append(result)
            print(f"{topic}: build + {len(runs)} invocation(s) PASS")
    if args.report:
        args.report.write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"{len(results)} projects checked; models do not validate company hardware or SDKs.")


if __name__ == "__main__":
    main()

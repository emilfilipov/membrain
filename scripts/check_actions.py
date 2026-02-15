#!/usr/bin/env python3
import argparse
import json
import os
import re
import subprocess
import sys
import urllib.request
from urllib.error import HTTPError


def read_pat() -> str:
    env_pat = os.getenv("GITHUB_PAT")
    if env_pat:
        return env_pat.strip()

    candidates = [
        os.path.expanduser("~/.secrets/github_pat.env"),
        os.path.expanduser("~/.secrects/github_pat.env"),
    ]
    for secret_path in candidates:
        if not os.path.exists(secret_path):
            continue
        with open(secret_path, "r", encoding="utf-8") as handle:
            for line in handle:
                if line.startswith("GITHUB_PAT="):
                    return line.split("=", 1)[1].strip()
    return ""


def detect_repo() -> str:
    env_repo = os.getenv("GITHUB_REPO")
    if env_repo:
        return env_repo.strip()
    try:
        output = subprocess.check_output(
            ["git", "config", "--get", "remote.origin.url"],
            stderr=subprocess.DEVNULL,
            text=True,
        ).strip()
    except Exception:
        return ""
    if not output:
        return ""
    if output.startswith("git@"):
        match = re.search(r"github\.com:(.+?)(?:\.git)?$", output)
    else:
        match = re.search(r"github\.com/([^/]+/[^/]+?)(?:\.git)?$", output)
    return match.group(1) if match else ""


def api_get(url: str, pat: str) -> dict:
    req = urllib.request.Request(
        url,
        headers={
            "Authorization": f"token {pat}",
            "Accept": "application/vnd.github+json",
            "User-Agent": "membrain-ci-check",
        },
    )
    with urllib.request.urlopen(req) as resp:
        return json.load(resp)


def print_runs(data: dict) -> list:
    runs = data.get("workflow_runs", [])
    print(f"Found {len(runs)} runs")
    for run in runs:
        print(
            "- id={id} name={name} status={status} conclusion={conclusion} "
            "event={event} created_at={created_at} html_url={html_url}".format(
                id=run.get("id"),
                name=run.get("name"),
                status=run.get("status"),
                conclusion=run.get("conclusion"),
                event=run.get("event"),
                created_at=run.get("created_at"),
                html_url=run.get("html_url"),
            )
        )
    return runs


def print_jobs(repo: str, pat: str, run_id: int) -> None:
    jobs_url = f"https://api.github.com/repos/{repo}/actions/runs/{run_id}/jobs?per_page=100"
    jobs = api_get(jobs_url, pat)
    print("\nJobs:")
    for job in jobs.get("jobs", []):
        print(
            "  - name={name} status={status} conclusion={conclusion} "
            "started_at={started_at} completed_at={completed_at} html_url={html_url}".format(
                name=job.get("name"),
                status=job.get("status"),
                conclusion=job.get("conclusion"),
                started_at=job.get("started_at"),
                completed_at=job.get("completed_at"),
                html_url=job.get("html_url"),
            )
        )


def main() -> int:
    parser = argparse.ArgumentParser(description="Check latest GitHub Actions runs.")
    parser.add_argument("--repo", help="owner/repo (defaults to origin remote).")
    parser.add_argument("--per-page", type=int, default=5, help="Runs to fetch.")
    parser.add_argument("--jobs", action="store_true", help="Print jobs for latest run.")
    parser.add_argument("--run-id", type=int, help="Run ID to fetch jobs for.")
    args = parser.parse_args()

    pat = read_pat()
    if not pat:
        print("Missing PAT. Set GITHUB_PAT or ~/.secrets/github_pat.env (also checks ~/.secrects/github_pat.env)", file=sys.stderr)
        return 1

    repo = (args.repo or detect_repo()).strip()
    if not repo:
        print("Missing repo. Use --repo owner/repo or set GITHUB_REPO.", file=sys.stderr)
        return 1

    url = f"https://api.github.com/repos/{repo}/actions/runs?per_page={args.per_page}"
    try:
        data = api_get(url, pat)
    except HTTPError as exc:
        print(f"HTTP error: {exc.code} {exc.reason}", file=sys.stderr)
        return 1

    runs = print_runs(data)
    run_id = args.run_id or (runs[0].get("id") if runs else None)
    if args.jobs and run_id:
        print_jobs(repo, pat, int(run_id))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

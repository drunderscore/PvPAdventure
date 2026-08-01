# Manually Import a PvPAdventure Match Backup

PvPAdventure saves every completed match here:

```text
/tModLoader/PvPAdventureMatchesBackupDoNotDelete/
```

Each `.json` file is complete match payload for Tavernkeep.

Backup exists because server sometimes fail. Authentication fail, upload fail, internet fail. File remains even when upload succeeds, so you must check before importing. Importing twice may duplicate match, rewards, and achievements. Very bad.

First, set file and confirm JSON is valid:

```bash
MATCH_JSON='/absolute/path/to/match.json'
jq empty "$MATCH_JSON"
```

Back up Tavernkeep database before proceeding.

## Check Whether Match Already Exists

From Tavernkeep directory:

```bash
MATCH_TOKEN="$(jq -r '.metrics.match_token // empty' "$MATCH_JSON")"
test -n "$MATCH_TOKEN" || exit 1

sqlite3 ./data/tavernkeep.db \
  "SELECT match_id FROM match_metric WHERE \"key\" = 'match_token' AND value = '$MATCH_TOKEN';"
```

If command prints match ID, stop. Match already imported. Do not import twice.

## Import Match

Send backup unchanged to trusted local Tavernkeep process:

```bash
curl --fail-with-body --silent --show-error \
  --request POST 'http://127.0.0.1:8080/match/v1' \
  --header 'Content-Type: application/json' \
  --header 'Official-Verified: SUCCESS' \
  --header 'Official-Identity-Subject: MANUAL_BACKUP_IMPORT' \
  --header 'Official-Identity-Fingerprint: MANUAL_BACKUP_IMPORT' \
  --data-binary "@$MATCH_JSON"
```

Keep port `8080` private. These headers must never travel through public internet.

Success returns HTTP `200` and new match ID. Run duplicate check again; it should print exactly one ID.

If import fails, read response and Tavernkeep logs. Do not retry until duplicate check confirms no match was committed.

Do not insert rows manually into SQLite. Tavernkeep endpoint restores match, players, teams, stats, rewards, and achievements together.

Backup contains JSON only, not `.reese` replay. `/match/v1` restores database data but no replay URL. Use `/match/v2` if replay also needs recovery.

Players without Steam IDs cannot become valid Tavernkeep players. Even Erky cannot import identity that never existed.
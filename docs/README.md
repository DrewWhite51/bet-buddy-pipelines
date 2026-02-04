# Documentation

## Pipelines

Per-scraper documentation covering target sites, URL patterns, parsed fields, and known quirks.

- [DraftKings](pipelines/draftkings.md)

## Database

- [AWS RDS Setup](aws-rds-setup.md) - setting up a free-tier PostgreSQL instance in AWS
- [Database Migrations](database-migrations.md) - creating, applying, and managing EF Core migrations

## Analytics

- [AWS Athena & Glue Setup](aws-athena-glue-setup.md) - query S3 odds data with SQL using Athena and Glue

## Architecture

System diagrams rendered with [Mermaid](https://mermaid.js.org/).

- [System Overview](architecture/system-overview.md) - component-level view of the full system
- [Data Flow](architecture/data-flow.md) - runtime sequence from Lambda invocation to S3 write

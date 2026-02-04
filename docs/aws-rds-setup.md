# AWS RDS PostgreSQL Setup

Step-by-step guide to setting up a free-tier PostgreSQL instance for the Sports Betting Pipeline.

## 1. Navigate to RDS

- Log in to the [AWS Console](https://console.aws.amazon.com)
- Search for "RDS" in the top search bar and open it
- Make sure your region is set to **US East (N. Virginia) / us-east-1** in the top-right corner

## 2. Create Database

- Click **Create database**
- Choose **Standard create**
- Engine type: **PostgreSQL**
- Engine version: **PostgreSQL 16** (latest 16.x)

## 3. Templates

- Select **Free tier** (this auto-selects db.t3.micro and disables Multi-AZ)

## 4. Settings

- **DB instance identifier:** `sportsbetting-db`
- **Master username:** `postgres`
- **Credentials management:** Self managed
- **Master password:** choose a strong password and save it somewhere safe

## 5. Instance Configuration

- Should already be set to **db.t3.micro** (free tier)
- If not, select it manually

## 6. Storage

- **Storage type:** gp2 (General Purpose SSD)
- **Allocated storage:** 20 GB
- **Uncheck** "Enable storage autoscaling" (keeps costs predictable)

## 7. Connectivity

- **Compute resource:** Don't connect to an EC2 instance
- **VPC:** Default VPC
- **Public access:** **Yes** (needed so your Lambda and local machine can connect)
- **VPC security group:** Create new
  - Name it `sportsbetting-db-sg`
- **Availability Zone:** No preference
- **Database port:** 5432

## 8. Database Authentication

- Select **Password authentication**

## 9. Additional Configuration

- **Initial database name:** `sportsbetting`
- **Uncheck** "Enable automated backups" (saves storage costs in free tier)
- **Uncheck** "Enable Enhanced monitoring" (avoids extra charges)
- **Uncheck** "Enable deletion protection" (for a dev instance)

## 10. Create

- Click **Create database**
- Wait for the status to change to **Available** (usually a few minutes)

## 11. Configure Security Group

Once the database is available:

- Click on your database instance name
- Scroll to **Connectivity & security**
- Click the security group link under **VPC security groups**
- Click **Inbound rules** tab, then **Edit inbound rules**
- Add a rule:
  - **Type:** PostgreSQL
  - **Port:** 5432
  - **Source:** `0.0.0.0/0` (for dev — restrict this for production)
- Click **Save rules**

## 12. Get Your Connection String

- Go back to your RDS instance
- Copy the **Endpoint** from the Connectivity section (e.g., `sportsbetting-db.abc123xyz.us-east-1.rds.amazonaws.com`)
- Your connection string is:

```
Host=<endpoint>;Port=5432;Database=sportsbetting;Username=postgres;Password=<your-password>
```

## 13. Configure the Project

### Lambda (production)

Set the environment variable in the Lambda console:

- Open your Lambda function in the AWS console
- Go to **Configuration** > **Environment variables**
- Add: `Database__ConnectionString` = `Host=<endpoint>;Port=5432;Database=sportsbetting;Username=postgres;Password=<your-password>`

### Sandbox (local dev)

Update `src/Sandbox/appsettings.json`:

```json
{
  "Database": {
    "ConnectionString": "Host=<endpoint>;Port=5432;Database=sportsbetting;Username=postgres;Password=<your-password>"
  }
}
```

## 14. Verify Connection

Test the connection locally:

```bash
# If you have psql installed
psql "host=<endpoint> port=5432 dbname=sportsbetting user=postgres password=<your-password>"
```

## 15. Run Migrations

Once connected, create and apply the initial migration:

```bash
cd SportsBettingPipeline

dotnet ef migrations add InitialCreate \
  --project src/Infrastructure \
  --startup-project src/Sandbox

dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Sandbox
```

## Cost Summary

| Resource | Free Tier | After Free Tier |
|---|---|---|
| db.t3.micro | 750 hrs/month for 12 months | ~$15/month |
| Storage (20 GB gp2) | 20 GB for 12 months | ~$2.30/month |
| Data transfer | 1 GB/month outbound | $0.09/GB |

## Security Notes (for later)

For production, you should:
- Set **Public access** to No and use VPC networking with Lambda
- Restrict the security group to specific IPs or security groups
- Use AWS Secrets Manager for the password instead of env vars
- Enable automated backups
- Enable deletion protection

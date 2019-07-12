
### Features

- Supports clusters of pools each running individual currencies
- Ultra-low-latency, multi-threaded Stratum implementation using asynchronous I/O
- Adaptive share difficulty ("vardiff")
- PoW validation (hashing) using native code for maximum performance
- Session management for purging DDoS/flood initiated zombie workers
- Payment processing
- Banning System
- Live Stats [API](https://github.com/aionnetwork/aion_pool3/wiki/API) on Port 4000
- WebSocket streaming of notable events like Blocks found, Blocks unlocked, Payments and more
- POW (proof-of-work) & POS (proof-of-stake) support
- Detailed per-pool logging to console & filesystem
- Runs on Linux and Windows

- Sending payments using private key
- Minimum payment configuration for each individual miner
- Invalid shares (displayed in the UI + recorder for the backend)
- UX for navigating to Explorer from transaction hash and miner address
- Network difficulty graph
- Total Aion paid graph


### Runtime Requirements on Windows

- [.Net Core 2.2 Runtime](https://www.microsoft.com/net/download/core)
- [PostgreSQL Database](https://www.postgresql.org/)
- Coin Daemon (per pool)

### Runtime Requirements on Linux

- [.Net Core 2.2 SDK](https://www.microsoft.com/net/download/core)
- [PostgreSQL Database](https://www.postgresql.org/)
- Coin Daemon (per pool)
- Miningcore needs to be built from source on Linux. Refer to the section further down below for instructions.



### PostgreSQL Database setup

Create the database:

```console
$ create user miningcore
$ create db miningcore
$ psql (enter the password for postgres)
```

Run the query after login:

```sql
alter user miningcore with encrypted password 'some-secure-password';
grant all privileges on database miningcore to miningcore;
```

Import the database schema:

```console
$ wget https://github.com/aionnetwork/aion_pool3/blob/master/aion_pool/src/Miningcore/Persistence/Postgres/Scripts/cleandb.sql
$ psql -d miningcore -U miningcore -f createdb.sql
```

### [Configuration](https://github.com/aionnetwork/aion_pool3/wiki/Configuration)

### [API](https://github.com/aionnetwork/aion_pool3/wiki/API)

### Building from Source

#### Building on Ubuntu 16.04

```console
$ wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb
$ sudo apt-get update -y
$ sudo apt-get install apt-transport-https -y
$ sudo apt-get update -y
$ sudo apt-get -y install dotnet-sdk-2.1 git cmake build-essential libssl-dev pkg-config libboost-all-dev libsodium-dev libzmq5 libzmq3-dev
$ git clone https://github.com/aionnetwork/aion_pool3.git
$ cd aion_pool/src/Miningcore
$ dotnet publish -c Release --framework netcoreapp2.1  -o ../../build
```

#### Building on Windows

Download and install the [.Net Core 2.1 SDK](https://www.microsoft.com/net/download/core)

```dosbatch
> git clone https://github.com/aionnetwork/aion_pool3.git
> cd aion_pool/src/Miningcore
> dotnet publish -c Release --framework netcoreapp2.1  -o ..\..\build
```

#### Building on Windows - Visual Studio

- Download and install the [.Net Core 2.1 SDK](https://www.microsoft.com/net/download/core)
- Install [Visual Studio 2017](https://www.visualstudio.com/vs/). Visual Studio Community Edition is fine.
- Open `Miningcore.sln` in VS 2017


#### After successful build

Create a configuration file <code>config.json</code> as described [here](https://github.com/aionnetwork/aion_pool3/wiki/Configuration) or use [examples/aion_pool.json](https://github.com/aionnetwork/aion_pool3/blob/master/aion_pool/examples/aion_pool.json)

```
cd ../../build
dotnet Miningcore.dll -c config.json
```

## Running a production pool

A public production pool requires a web-frontend for your users to check their hashrate, earnings, etc. Follow the [instructions](https://github.com/aionnetwork/aion_pool3/tree/master/ui) for the UI installation  

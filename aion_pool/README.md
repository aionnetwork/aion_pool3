## Features

- Supports clusters of pools each running payout node and different stratum nodes
- Ultra-low-latency, multi-threaded Stratum implementation using asynchronous I/O
- Adaptive share difficulty ("vardiff")
- Session management for purging DDoS/flood initiated zombie workers
- Payment processing
- Banning System
- Live Stats [API](https://github.com/coinfoundry/miningcore/wiki/API) on Port 4000
- WebSocket streaming of notable events like Blocks found, Blocks unlocked, Payments and more
- POW (proof-of-work)
- Detailed per-pool logging to console & filesystem
- Runs on Linux and Windows

## Runtime Requirements

- [.Net Core 2.2 SDK](https://www.microsoft.com/net/download/core)
- [PostgreSQL Database](https://www.postgresql.org/)
- OAN Kernel
  - [Java Kernel](https://github.com/aionnetwork/aion/releases)
  - [Rust Kernel](https://github.com/aionnetwork/aionr/releases)
- (Optional) libzmq3-dev, libzmq5. If you want to setup Master-Cluster, you need to install zmq.

### Recommended Minimum Hardware Specifications
- SSD Based Storage
- 32GB RAM
- 16 core CPU

## Build
### System Dependencies
```
$ apt-get update -y 
$ apt-get -y install git cmake build-essential libssl-dev pkg-config libboost-all-dev libsodium-dev libzmq3-dev libzmq5
```

### Database Setup
Due to slight differences between AION desktop and server configurations, ensure the correct instruction set is followed based on your installation method. 

#### Ubuntu Desktop APT package manager:

PostgreSQL packages may be installed through the apt package mangager. Creating a postgres user under the current username may simplify database configuration; the following command creates that postgres user.

```sudo -u postgres createuser -s $USERNAME```

Choose a secure password for the miningcore user, this account will be the to store and process share payouts.

```console
$ createuser miningcore
$ createdb miningcore
$ psql miningcore
```

Run the query after login:

```sql
alter user miningcore with encrypted password '{PASSWORD}'; 
grant all privileges on database miningcore to miningcore;
```
\q (to quit)

Navigate back to the terminate and import the database schema (ensure you are at the root of the repository)

Import the database schema (make sure that you are in the root of the repositiory directory):

```console
$ cd src/MiningCore/Persistence/Postgres/Scripts
$ psql -d miningcore -f createdb.sql
```

#### Ubuntu Server APT package manager:


Choose a secure password for the miningcore user, this account will be the to store and process share payouts.

```console
$ sudo -u postgres createuser miningcore
$ sudo -u postgres createdb miningcore
$ sudo -u postgres psql
```

Run the query after login:

```sql
alter user miningcore with encrypted password '{PASSWORD}'; 
grant all privileges on database miningcore to miningcore;
```
\q (to quit)

Navigate back to the terminate and import the database schema (ensure you are at the root of the repository)

Import the database schema (make sure that you are in the root of the repositiory directory):

```console
$ cd src/MiningCore/Persistence/Postgres/Scripts
$ sudo -u postgres psql -d miningcore -f createdb.sql
```

### Building from Source

```console
$ git clone https://github.com/aionnetwork/aion_pool3.git
$ cd aion_pool3/aion_pool/src/Miningcore
$ ./linux-build.sh
```

## Configuration
### Kernel Configuration
1. setup an aion account as payment account.
2. enable JsonRpc interface and enable net api.
3. Increase the number of RPC server threads to 8.

The number minimum number of RPC threads should be 4, although it is recommended that the number of threads be increased to 8. Failure to increase the number of threads may cause RPC requests to become queued and eventually dropped.

4. Replace the miner address with your account address; this is the address which will receive mining rewards.

For details, please refer to Java and Rust kernel guides.

## [Pool Configuration](./docs/Configuration.md)

## Start Pool
#### Start miner
```
$ ./miner --algo aion --server <stratum_server> --port <stratum_port> --user <miner_address>.<worker_name> --pass mp=<minimum_payment>;d=<static_diff>
```
* miner_address - miner address to receive payment. 
* worker_name - optional.
* minimum_payment - pool will send payment after your balance is more than this value.

Example:
```
$ ./miner --algo aion --server localhost --port 3333 --user 0xa0f499fe8fc35c31b0c8a802d947744d765f7c555d01b2b69ef7a9d894bbbfd4.w1 --pass mp=15
```

#### Start kernel

#### Start pool
```shell
$ cd ../../build
$ dotnet Miningcore.dll -c aion_pool.json
```

# Database Migration from aion_pool2
1. remove ``payoutinfo`` column in ``shares`` table
2. remove ``coin`` column in ``balances`` table
3. remove ``coin`` in ``balance_changes`` table
4. add table ``miner_info``
```sql
create table if not exists miner_info (
	poolid text not null,
	miner text not null,
	minimumpayment decimal(28, 12) NOT NULL DEFAULT 0
);
```

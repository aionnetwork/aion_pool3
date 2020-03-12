## Data Source
```json
"persistence": {
  "postgres": {
    "host": "127.0.0.1",
    "port": 5432,
    "user": "miningcore",
    "password": "password",
    "database": "miningcore"
  }
}
```

## Aion Pool
```json
"pools": [{
  "id": "aion-pool",
  "enabled": true,
  "coin": "aion",
  "address": "0xa02e4f1e4c192a0a992eb5d4d30e6359b800a3759e1d749c6c0b591ab8e47fc9",
  "rewardRecipients": [
    {
      "address": "0xa02e4f1e4c192a0a992eb5d4d30e6359b800a3759e1d749c6c0b591ab8e47fc9",
      "percentage": 1
    }
  ],
}]
```
**NOTE: BlockRreshInterval pool configuration is deprecated, 200ms is uesd internally be default.**

## Payout
```json
"paymentProcessing": {
  "enabled": true,
  "minimumPayment": 0.1,
  "enableMinerMinimumPayment": true,
  "payoutScheme": "PPLNS",
  "payoutSchemeConfig": {
    "factor": 2.0
  },
  "accountPassword": "password",
  "keepTransactionFees": true,
  "minimumConfirmations": 10,
  "nrgFee": 0.0001,
  "minimumPeerCount": 1
}
```
### minimum payment
Minimum payment is the amount which is used as a threshold for sending payout to miners.

Miners are also allowed to configure a higher minimum payment than the one defined by the pool. This would be set using the connection string to the pool.
```shell
$ $ ./miner --algo aion --server localhost --port 3333 --user 0xa0f499fe8fc35c31b0c8a802d947744d765f7c555d01b2b69ef7a9d894bbbfd4.w1 --pass mp=1
```
In order for the miner minimum payment setting to work the following configuration needs to be changed:
* enableMinerMinimumPayment: true
* minimumPayment: 0.1

## Cluster
### Master
Master's responsibility:
1. payment
2. provide stats api
3. update database
```json
"relays": [
  {
    "url": "tcp://127.0.0.1:6000",
    "sharedEncryptionKey": "testkey"
  }
],
"api": {
  "enabled": true,
  "listenAddress": "*",
  "port": 4000
},
"paymentProcessing": {
  "enabled": true,
  "interval": 60,
  "shareRecoveryFile": "recovered-shares.txt"
},
"pools":[
  {
    "id": "aion-pool",
    "enabled": true,
    ...
    "ports": {}
  }
]
```
* enable payment processing
* accept relaying from cluster nodes
* enable dashboard api
* disable stratum ports

### Slave
slave responsibility:
1. stratum
2. relay shares
```json
"relay": {
  "publishUrl": "tcp://0.0.0.0:6000",
  "sharedEncryptionKey": "testkey"
},
"persistence": {},
"paymentProcessing": {
  "enabled": false
},
"api": {
  "enabled": false
},
"pools": [{
  "id": "aion-pool",
  "enabled": true,
  "ports": {
    "3333": {
      "listenAddress": "0.0.0.0",
      "difficulty": 128,
      "varDiff": {
        "minDiff": 128,
        "maxDiff": 2048,
        "targetTime": 5,
        "retargetTime": 5,
        "variancePercent": 30,
        "maxDelta": 500
      }
    }
  }
}]
```
* enable relay endpoint
* disable persistence configuration
* disable payment processing
* disable dashboard api
* enable stratum port
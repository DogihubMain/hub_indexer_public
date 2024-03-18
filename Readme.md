### Prerequisites

- Dogecoin node with RPC and txindex enable in dogecoin.conf
- Redis database with at least 64Go of ram (128 would be better)

Some advices : 
- Dogecoin node : Adapt the rpcworkqueue value depending on your server ressources in dogecoin.conf
- Dogecoin node : To avoid latences it could be great that rpcworkqueue has the same value as the number of rpc client in the indexer pool

# DoginalsHub Indexer - BETA (experimental for now)

App is in C# and use NBitcoin package to communicate with an RPC Dogecoin core node

Once compiled, use --help to have all commands available

### Redis database locally

To run redis database locally by persisting data use the following command (require Docker) : 

    docker compose -f .\docker-compose.redis.yml up

### Run the indexer

First you have to clone the repo on the machine.

Install dotnet sdk 7 on your host 

Then, to build the app use (for linux target):

    dotnet publish -c Release -r linux-x64 --self-contained true

You will find the result inside the following folder : 

    DogiHubIndexer/bin/Release/net7.0/linux-x64

Allow the binary to be executed :

    chmod +x DogiHubIndexer
	
We recommanded to create a /app folder in your host with the following subfolders : 
- indexer
- node  (for startup mode we recommand to have the dogecoin node locally for performance)
- redis (for startup mode we recommand to have the redis locally for performance)
- logs (indexer will store logs files in here)

Create a app/run_indexer.sh file with the following content (adjust parameters for your needs) : 

    #!/bin/bash

    /app/indexer/DogiHubIndexer/bin/Release/net7.0/linux-x64/DogiHubIndexer \
      --firstInscriptionBlockHeight "4609723" \
	  --lastStartupBlockHeight "4730000" \
      --rpcUrl "http://localhost:22555" \
      --rpcUsername "dogihub" \
      --rpcPassword "D0g1hUbNodeToTh3moONNNNNNNNN" \
      --rpcPoolSize "100" \
      --redisConnectionString "localhost:6379,password=dogiHuBToTh3mO0nS00nnNN,abortConnect=false,allowAdmin=true" \
      --flushRedis "false" \
      --pendingConfirmationNumber "12" \
      --cpuNumber "24" \
      --redisDataFolder "/app/redis/redis_data/" \
	  --logFilePath "/app/logs/dogihubindexer-.log" \
	  --inscriptionTypes Token Nft Dogemap Dns
      --mode "startup" &

Use allowAdmin=true in the redis connection string to enable flush databases options

Then allow the run_indexer.sh to be executed with:

    sudo chmod +x run_indexer.sh

Finaly run the indexer in background with : 

    nohup ./run_indexer.sh &

To see the logs use (replace the YYYYMMDD by your current log date, a new log file is created every day) : 

    tail -f -n 20 /app/logs/dogihubindexer-YYYYMMDD.log

If you want to stop the indexer find its process id with : 

    ps aux | grep 'DogiHubIndexer'

and just use the kill command on the corresponding id :

    kill 12345
	
### Parameters

#### firstInscriptionBlockHeight
First block containing an inscription

#### lastStartupBlockHeight
Last block to parse during startup mode (higher block height is taken from blockchain if null)

#### rpcUrl
RPC Url

#### rpcUsername
RPC Username

#### rpcPassword
RPC Password

#### rpcPoolSize
Number of RPC Client in the pool

#### redisConnectionString
Redis connection string

#### flushRedis
For testing purpose, flush all redis databases (must be admin)

#### pendingConfirmationNumber
Number of pending blocks to confirm a transfer in a balance 
(default 16)

#### cpuNumber
Host CPU number for parallelism

#### redisDataFolder
Redis data folder (used for backup system)

#### logFilePath
File path of logs

#### mode
Startup or Daemon mode are available 

### inscriptionTypes

inscriptionTypes field allow you to chose which type of inscription you want to parse
available values : Token, Dogemap, Nft, Dns
(to parse multiple type at the same time just write them separate by a space)

### numberOfBlockBehindBlockchain
To avoid frequent reorgs you can chose the number of block you want to stay behind the blockchain during daemon mode
(default 0)

### startupAutomaticDumpStep
Automatically dump redis db each x blocks during startup mode
(default 10000)

### deleteTransactionHistory
Decide if you delete all related inscriptions transactions history to lighten redis

### License

See License file on the root repo

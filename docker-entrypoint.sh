#!/bin/bash
redis-server --dir /redis_data --maxmemory-policy noeviction --tracking-table-max-keys 100000 --loglevel warning --requirepass yourpass &

dotnet DogiHubIndexer.dll --firstInscriptionBlockHeight "4630091" \
  --rpcUrl "http://host.docker.internal:22555" \
  --rpcUsername "user" \
  --rpcPassword "pass" \
  --rpcPoolSize "100" \
  --redisConnectionString "localhost:6379,password=yourpass,abortConnect=false,allowAdmin=true" \
  --flushRedis "false" \
  --pendingConfirmationNumber "12" \
  --cpuNumber "24" \
  --redisDataFolder "/redis_data/" \
  --logFilePath "/app/logs/dogihubindexer-.log" \
  --inscriptionTypes Token \
  --startupAutomaticDumpStep 10000 \
  --mode "startup"

#RUN ON LINUX : 
#docker run -d -p 6379:6379 -v ./redis_data:/redis_data -v ./indexer_logs:/app/logs dogihub-indexer

#RUN ON WINDOWS : 
#docker run -d -p 6379:6379 -v C:/GitHub/hub_indexer/redis_data:/redis_data -v C:/GitHub/hub_indexer/indexer_logs:/app/logs dogihub-indexer

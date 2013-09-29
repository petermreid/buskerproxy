netsh http delete sslcert ipport=0.0.0.0:8081
netsh http add sslcert ipport=0.0.0.0:8081 certhash=349eedda04262807f9adb6a532df050c0f3c4be0 appid={29509e81-9cea-4031-a8de-e165078bf99a} usagecheck=disable


### Use-cases:
* Add broker  
  * `localhost:5164/add-broker?login=admin&password=Zaqwsx1%40`  

* Update broker
  * `localhost:5164/update-broker?id=<broker_id>&login=admin&password=Zaqwsx1%40`
  * `localhost:5164/update-broker?id=a40eba11-a903-48cb-8c6d-c96a2f97964c&login=admin&password=Zaqwsx1%40`
  
* Subscribe topic:  
  * `localhost:5164/subscribe-topic?brokerId=f7846e2c-283c-49cb-aeed-28271dbd2727&topic=test`

* Stop broker:  
  * `localhost:5164/stop-broker?id=f7846e2c-283c-49cb-aeed-28271dbd2727`

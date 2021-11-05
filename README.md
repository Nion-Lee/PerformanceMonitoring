# PerformanceMonitoring
WorkerService為伺服器端程式，其以Worker服務實作，裏頭分別內建兩個服務「監測當前機器效能」與「Socket伺服器」  
效能監測能檢測每秒當前CPU、RAM、Disk使用量，以及當前進程數量  
而Socket伺服器則保持監聽狀態，能夠讓連線進來的客戶端實時獲取當前數據最新狀態（Socket Server & Client皆以非同步方法實作）

# PerformanceMonitoring
WorkerService為伺服器端程式--  
其以Worker服務實作，裏頭分別內建兩個服務「監測當前機器效能」與「Socket伺服器」  
  
效能監測服務：檢測每秒當前CPU、RAM、Disk使用量，讀寫請求數與當前進程數量  
Socket伺服器：保持監聽狀態，使連線成功之客戶端獲取每秒當前最新數據（Server/Client皆以非同步方式實作）
  
另Socket相關參數、監控指標閾值，皆以json配置文件綁定，方便使用者修改參數  
最後資料於Client端控台上輸出時，訊息採用同位置覆蓋方式，而非整個畫面Console.Clear()

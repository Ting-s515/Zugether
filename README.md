<!DOCTYPE html>
<html lang="zh-Hant">

<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>

<body>
    <h1>Zugether合租網站</h1>
    <h2>目錄 : </h2>
    <ul>
        <li><a href="#project-description">專案說明</a></li>
        <li><a href="#responsible-for-project">我負責的項目</a></li>
        <li><a href="#technology-application">技術應用</a></li>
        <li><a href="#database-design">資料庫設計</a></li>
        <li><a href="#function-description">功能說明</a></li>
    </ul>
    <h2 id="project-description">專案說明 : </h2>
    <ul>
        <li>目標是幫助人們快速找到志同道合的室友，以更低租金、更大空間，讓生活更加舒適，旨在為租房族提供一個方便且可靠的管道，來滿足租房的需求。無論是希望找到志同道合的合租夥伴，還是想更深入地了解潛在的室友，我們的平台都能滿足您的需求。
        </li>
    </ul>
    <h2 id="responsible-for-project">我負責的項目 : </h2>
    <ul>
        <li>房間頁面畫面排版: 實現直觀的介面，使用bootstrap實現RWD。</li>
        <li>搜尋房間: 提供多條件篩選，使用 LINQ 實現高效的資料查詢。</li>
        <li>google登入: 串接Google OAuth，以便讓用戶加快登入流程。</li>
        <li>留言板: 使用SingnalR技術，實現即時雙向通訊功能。</li>
        <li>我的收藏: 開發房源收藏功能，讓用戶能保存感興趣的房源。</li>
    </ul>
    <h2 id="technology-application">技術應用 :　</h2>
    <ul>
        <li>前端 :</li>
        <ul>
            <li>HTML/CSS</li>
            <li>JS & jQuery</li>
            <li>Bootstrap</li>
        </ul>
        <li>後端</li>
        <ul>
            <li>ASP.NET MVC</li>
        </ul>
        <li>資料庫:</li>
        <ul>
            <li>MSSQL</li>
        </ul>
        <li>API:</li>
        <ul>
            <li>Google OAuth</li>
            <li>Google Maps</li>
        </ul>
        <li>其他:</li>
        <ul>
            <li>Google OAuth</li>
            <li>Google Maps</li>
            <li>SignalR</li>
            <li>Swiper</li>
            <li>Animate.css</li>
        </ul>
    </ul>
    <h2 id="database-design">資料庫設計 : </h2>
    <img src="https://i.imgur.com/18V86Ln.png" alt="資料庫設計圖" style="max-width: 100%; height: auto;">
    <h2 id="function-description">功能說明 :</h2>
    <ul>
        <li>
            房源搜尋：用戶可以輕鬆發布房源，並透過多重篩選功能快速找到符合需求的合租物件。
        </li>
        <li>
            Google 快速登入：整合 Google OAuth 提供快速登入功能，簡化註冊流程，提升用戶體驗。
        </li>
        <li>
            地圖整合：使用 Google Maps API 將房源位置直觀呈現，讓用戶更輕鬆地確認房源周邊環境。
        </li>
        <li>
            即時留言功能：會員之間可以即時留言互動，促進雙方溝通，提升資訊透明度。
        </li>
        <li>
            動畫效果優化：利用 Animate.css 增加頁面動態效果，提供更吸引人的視覺體驗。
        </li>
    </ul>
</body>

</html>

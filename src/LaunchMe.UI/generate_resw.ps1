# generate_resw.ps1 — Generates all 18 fully translated Resources.resw files
# Usage: .\generate_resw.ps1  (run from src/LaunchMe.UI)

$ErrorActionPreference = "Stop"
$baseDir = Join-Path $PSScriptRoot "Strings"

$translations = [ordered]@{
    "en-US" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Launcher"; "DockTitle"="Dock"; "MusicPlayerTitle"="Player";
        "SearchPlaceholder"="Search apps, files, and more…";
        "WidgetClock"="Clock"; "WidgetWeather"="Weather"; "WidgetCalendar"="Calendar"; "WidgetMusic"="Music";
        "WidgetPomodoro"="Focus Timer"; "WidgetPhoto"="Photo"; "WidgetAffirmation"="Affirmation";
        "WidgetSystemStats"="System Stats"; "WidgetPixelArt"="Pixel Art"; "WidgetShapePhoto"="Shape Photo"; "WidgetGif"="GIF";
        "SpaceDefault"="Default Space"; "SpaceNewName"="New Space";
        "WorkflowTitle"="Workflows"; "WorkflowNew"="New Workflow"; "WorkflowRun"="Run";
        "AgentApproveTitle"="Agent Request"; "AgentApprove"="Allow"; "AgentDeny"="Deny";
        "SettingsTitle"="Settings"; "SettingsGeneral"="General"; "SettingsAppearance"="Appearance"; "SettingsNotifications"="Notifications";
        "PhoneLinkTitle"="Phone Link"; "PhoneLinkPair"="Pair New Device"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Layout";
        "CloseButton"="Close"; "OkButton"="OK"; "CancelButton"="Cancel";
        "NotchClipboard"="Clipboard"; "NotchPocket"="Pocket Shelf"; "NotchNearbyShare"="Nearby Share"; "NotchFocusMode"="Focus Mode";
        "MusicModeVinyl"="Vinyl"; "MusicModeFullArt"="Full Screen Art"; "MusicModeParticles"="Particles"; "MusicModeGlowingBeats"="Glowing Beats"
    }
    "fr-FR" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Lanceur"; "DockTitle"="Dock"; "MusicPlayerTitle"="Lecteur";
        "SearchPlaceholder"="Rechercher applications, fichiers et plus…";
        "WidgetClock"="Horloge"; "WidgetWeather"="Météo"; "WidgetCalendar"="Calendrier"; "WidgetMusic"="Musique";
        "WidgetPomodoro"="Minuteur Focus"; "WidgetPhoto"="Photo"; "WidgetAffirmation"="Affirmation";
        "WidgetSystemStats"="Stats Système"; "WidgetPixelArt"="Pixel Art"; "WidgetShapePhoto"="Photo Forme"; "WidgetGif"="GIF";
        "SpaceDefault"="Espace par défaut"; "SpaceNewName"="Nouvel espace";
        "WorkflowTitle"="Flux de travail"; "WorkflowNew"="Nouveau flux"; "WorkflowRun"="Exécuter";
        "AgentApproveTitle"="Demande d'agent"; "AgentApprove"="Autoriser"; "AgentDeny"="Refuser";
        "SettingsTitle"="Paramètres"; "SettingsGeneral"="Général"; "SettingsAppearance"="Apparence"; "SettingsNotifications"="Notifications";
        "PhoneLinkTitle"="Lien Mobile"; "PhoneLinkPair"="Associer un appareil"; "PhoneLinkPairPin"="Code PIN : {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Disposition";
        "CloseButton"="Fermer"; "OkButton"="OK"; "CancelButton"="Annuler";
        "NotchClipboard"="Presse-papiers"; "NotchPocket"="Étagère Pocket"; "NotchNearbyShare"="Partage de proximité"; "NotchFocusMode"="Mode Concentration";
        "MusicModeVinyl"="Vinyle"; "MusicModeFullArt"="Pochette Plein Écran"; "MusicModeParticles"="Particules"; "MusicModeGlowingBeats"="Rythmes Lumineux"
    }
    "de-DE" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Starter"; "DockTitle"="Dock"; "MusicPlayerTitle"="Musikplayer";
        "SearchPlaceholder"="Apps, Dateien und mehr suchen…";
        "WidgetClock"="Uhr"; "WidgetWeather"="Wetter"; "WidgetCalendar"="Kalender"; "WidgetMusic"="Musik";
        "WidgetPomodoro"="Fokus-Timer"; "WidgetPhoto"="Foto"; "WidgetAffirmation"="Affirmation";
        "WidgetSystemStats"="Systemstatus"; "WidgetPixelArt"="Pixel-Kunst"; "WidgetShapePhoto"="Form-Foto"; "WidgetGif"="GIF";
        "SpaceDefault"="Standardbereich"; "SpaceNewName"="Neuer Bereich";
        "WorkflowTitle"="Arbeitsabläufe"; "WorkflowNew"="Neuer Ablauf"; "WorkflowRun"="Ausführen";
        "AgentApproveTitle"="Agenten-Anfrage"; "AgentApprove"="Erlauben"; "AgentDeny"="Ablehnen";
        "SettingsTitle"="Einstellungen"; "SettingsGeneral"="Allgemein"; "SettingsAppearance"="Darstellung"; "SettingsNotifications"="Benachrichtigungen";
        "PhoneLinkTitle"="Smartphone-Link"; "PhoneLinkPair"="Gerät koppeln"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Layout";
        "CloseButton"="Schließen"; "OkButton"="OK"; "CancelButton"="Abbrechen";
        "NotchClipboard"="Zwischenablage"; "NotchPocket"="Pocket-Ablage"; "NotchNearbyShare"="Umgebungsfreigabe"; "NotchFocusMode"="Fokusmodus";
        "MusicModeVinyl"="Vinyl"; "MusicModeFullArt"="Vollbild-Cover"; "MusicModeParticles"="Partikel"; "MusicModeGlowingBeats"="Leuchtende Beats"
    }
    "es-ES" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Lanzador"; "DockTitle"="Dock"; "MusicPlayerTitle"="Reproductor";
        "SearchPlaceholder"="Buscar aplicaciones, archivos y más…";
        "WidgetClock"="Reloj"; "WidgetWeather"="Clima"; "WidgetCalendar"="Calendario"; "WidgetMusic"="Música";
        "WidgetPomodoro"="Temporizador Focus"; "WidgetPhoto"="Foto"; "WidgetAffirmation"="Afirmación";
        "WidgetSystemStats"="Estado del Sistema"; "WidgetPixelArt"="Arte Píxel"; "WidgetShapePhoto"="Foto con Forma"; "WidgetGif"="GIF";
        "SpaceDefault"="Espacio predeterminado"; "SpaceNewName"="Nuevo espacio";
        "WorkflowTitle"="Flujos"; "WorkflowNew"="Nuevo flujo"; "WorkflowRun"="Ejecutar";
        "AgentApproveTitle"="Solicitud de agente"; "AgentApprove"="Permitir"; "AgentDeny"="Denegar";
        "SettingsTitle"="Configuración"; "SettingsGeneral"="General"; "SettingsAppearance"="Apariencia"; "SettingsNotifications"="Notificaciones";
        "PhoneLinkTitle"="Enlace Móvil"; "PhoneLinkPair"="Vincular dispositivo"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Diseño";
        "CloseButton"="Cerrar"; "OkButton"="Aceptar"; "CancelButton"="Cancelar";
        "NotchClipboard"="Portapapeles"; "NotchPocket"="Bandeja Pocket"; "NotchNearbyShare"="Compartir con cercanos"; "NotchFocusMode"="Modo Concentración";
        "MusicModeVinyl"="Vinilo"; "MusicModeFullArt"="Carátula Completa"; "MusicModeParticles"="Partículas"; "MusicModeGlowingBeats"="Ritmos Luminosos"
    }
    "it-IT" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Avviatore"; "DockTitle"="Dock"; "MusicPlayerTitle"="Lettore";
        "SearchPlaceholder"="Cerca app, file e altro…";
        "WidgetClock"="Orologio"; "WidgetWeather"="Meteo"; "WidgetCalendar"="Calendario"; "WidgetMusic"="Musica";
        "WidgetPomodoro"="Timer Focus"; "WidgetPhoto"="Foto"; "WidgetAffirmation"="Affermazione";
        "WidgetSystemStats"="Statistiche Sistema"; "WidgetPixelArt"="Pixel Art"; "WidgetShapePhoto"="Foto Sagomata"; "WidgetGif"="GIF";
        "SpaceDefault"="Spazio predefinito"; "SpaceNewName"="Nuovo spazio";
        "WorkflowTitle"="Flussi di lavoro"; "WorkflowNew"="Nuovo flusso"; "WorkflowRun"="Esegui";
        "AgentApproveTitle"="Richiesta agente"; "AgentApprove"="Consenti"; "AgentDeny"="Rifiuta";
        "SettingsTitle"="Impostazioni"; "SettingsGeneral"="Generale"; "SettingsAppearance"="Aspetto"; "SettingsNotifications"="Notifiche";
        "PhoneLinkTitle"="Collegamento Telefono"; "PhoneLinkPair"="Associa dispositivo"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Layout";
        "CloseButton"="Chiudi"; "OkButton"="OK"; "CancelButton"="Annulla";
        "NotchClipboard"="Appunti"; "NotchPocket"="Scaffale Pocket"; "NotchNearbyShare"="Condivisione nelle vicinanze"; "NotchFocusMode"="Modalità Focus";
        "MusicModeVinyl"="Vinile"; "MusicModeFullArt"="Copertina Intera"; "MusicModeParticles"="Particelle"; "MusicModeGlowingBeats"="Ritmi Luminosi"
    }
    "pt-BR" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Inicializador"; "DockTitle"="Dock"; "MusicPlayerTitle"="Reprodutor";
        "SearchPlaceholder"="Pesquisar aplicativos, arquivos e mais…";
        "WidgetClock"="Relógio"; "WidgetWeather"="Clima"; "WidgetCalendar"="Calendário"; "WidgetMusic"="Música";
        "WidgetPomodoro"="Timer de Foco"; "WidgetPhoto"="Foto"; "WidgetAffirmation"="Afirmação";
        "WidgetSystemStats"="Status do Sistema"; "WidgetPixelArt"="Pixel Art"; "WidgetShapePhoto"="Foto em Forma"; "WidgetGif"="GIF";
        "SpaceDefault"="Espaço Padrão"; "SpaceNewName"="Novo Espaço";
        "WorkflowTitle"="Fluxos"; "WorkflowNew"="Novo Fluxo"; "WorkflowRun"="Executar";
        "AgentApproveTitle"="Solicitação de Agente"; "AgentApprove"="Permitir"; "AgentDeny"="Negar";
        "SettingsTitle"="Configurações"; "SettingsGeneral"="Geral"; "SettingsAppearance"="Aparência"; "SettingsNotifications"="Notificações";
        "PhoneLinkTitle"="Vínculo com Celular"; "PhoneLinkPair"="Parear Dispositivo"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Layout";
        "CloseButton"="Fechar"; "OkButton"="OK"; "CancelButton"="Cancelar";
        "NotchClipboard"="Área de Transferência"; "NotchPocket"="Bandeja Pocket"; "NotchNearbyShare"="Compartilhar por Proximidade"; "NotchFocusMode"="Modo Foco";
        "MusicModeVinyl"="Vinil"; "MusicModeFullArt"="Capa em Tela Cheia"; "MusicModeParticles"="Partículas"; "MusicModeGlowingBeats"="Batidas Luminosas"
    }
    "ja-JP" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="ランチャー"; "DockTitle"="ドック"; "MusicPlayerTitle"="プレーヤー";
        "SearchPlaceholder"="アプリ、ファイルなどを検索…";
        "WidgetClock"="時計"; "WidgetWeather"="天気"; "WidgetCalendar"="カレンダー"; "WidgetMusic"="ミュージック";
        "WidgetPomodoro"="集中タイマー"; "WidgetPhoto"="写真"; "WidgetAffirmation"="アファメーション";
        "WidgetSystemStats"="システム統計"; "WidgetPixelArt"="ピクセルアート"; "WidgetShapePhoto"="シェイプ写真"; "WidgetGif"="GIF";
        "SpaceDefault"="デフォルトスペース"; "SpaceNewName"="新しいスペース";
        "WorkflowTitle"="ワークフロー"; "WorkflowNew"="新規ワークフロー"; "WorkflowRun"="実行";
        "AgentApproveTitle"="エージェント要求"; "AgentApprove"="許可"; "AgentDeny"="拒否";
        "SettingsTitle"="設定"; "SettingsGeneral"="一般"; "SettingsAppearance"="外観"; "SettingsNotifications"="通知";
        "PhoneLinkTitle"="スマートフォン連携"; "PhoneLinkPair"="新しいデバイスをペアリング"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="レイアウト";
        "CloseButton"="閉じる"; "OkButton"="OK"; "CancelButton"="キャンセル";
        "NotchClipboard"="クリップボード"; "NotchPocket"="ポケットシェルフ"; "NotchNearbyShare"="近距離共有"; "NotchFocusMode"="集中モード";
        "MusicModeVinyl"="レコード"; "MusicModeFullArt"="フルスクリーンアート"; "MusicModeParticles"="パーティクル"; "MusicModeGlowingBeats"="グロービート"
    }
    "zh-CN" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="启动台"; "DockTitle"="程序坞"; "MusicPlayerTitle"="播放器";
        "SearchPlaceholder"="搜索应用、文件及更多…";
        "WidgetClock"="时钟"; "WidgetWeather"="天气"; "WidgetCalendar"="日历"; "WidgetMusic"="音乐";
        "WidgetPomodoro"="专注计时器"; "WidgetPhoto"="照片"; "WidgetAffirmation"="每日寄语";
        "WidgetSystemStats"="系统状态"; "WidgetPixelArt"="像素艺术"; "WidgetShapePhoto"="形状照片"; "WidgetGif"="动图";
        "SpaceDefault"="默认空间"; "SpaceNewName"="新建空间";
        "WorkflowTitle"="工作流"; "WorkflowNew"="新建工作流"; "WorkflowRun"="运行";
        "AgentApproveTitle"="智能体请求"; "AgentApprove"="允许"; "AgentDeny"="拒绝";
        "SettingsTitle"="设置"; "SettingsGeneral"="通用"; "SettingsAppearance"="外观"; "SettingsNotifications"="通知";
        "PhoneLinkTitle"="手机连接"; "PhoneLinkPair"="配对新设备"; "PhoneLinkPairPin"="配对码：{0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="布局";
        "CloseButton"="关闭"; "OkButton"="确定"; "CancelButton"="取消";
        "NotchClipboard"="剪贴板"; "NotchPocket"="口袋暂存架"; "NotchNearbyShare"="就近共享"; "NotchFocusMode"="专注模式";
        "MusicModeVinyl"="黑胶唱片"; "MusicModeFullArt"="全屏封面"; "MusicModeParticles"="粒子视效"; "MusicModeGlowingBeats"="炫光节拍"
    }
    "zh-TW" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="啟動台"; "DockTitle"="Dock"; "MusicPlayerTitle"="播放器";
        "SearchPlaceholder"="搜尋應用程式、檔案與更多…";
        "WidgetClock"="時鐘"; "WidgetWeather"="天氣"; "WidgetCalendar"="行事曆"; "WidgetMusic"="音樂";
        "WidgetPomodoro"="專注計時器"; "WidgetPhoto"="相片"; "WidgetAffirmation"="每日肯定語";
        "WidgetSystemStats"="系統狀態"; "WidgetPixelArt"="像素藝術"; "WidgetShapePhoto"="形狀相片"; "WidgetGif"="GIF 動圖";
        "SpaceDefault"="預設空間"; "SpaceNewName"="新增空間";
        "WorkflowTitle"="工作流程"; "WorkflowNew"="新增工作流程"; "WorkflowRun"="執行";
        "AgentApproveTitle"="代理請求"; "AgentApprove"="允許"; "AgentDeny"="拒絕";
        "SettingsTitle"="設定"; "SettingsGeneral"="一般"; "SettingsAppearance"="外觀"; "SettingsNotifications"="通知";
        "PhoneLinkTitle"="手機連線"; "PhoneLinkPair"="配對新裝置"; "PhoneLinkPairPin"="配對碼：{0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="版面配置";
        "CloseButton"="關閉"; "OkButton"="確定"; "CancelButton"="取消";
        "NotchClipboard"="剪貼簿"; "NotchPocket"="Pocket 暫存架"; "NotchNearbyShare"="鄰近分享"; "NotchFocusMode"="專注模式";
        "MusicModeVinyl"="黑膠唱片"; "MusicModeFullArt"="全螢幕封面"; "MusicModeParticles"="粒子效果"; "MusicModeGlowingBeats"="霓虹節拍"
    }
    "ko-KR" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="런처"; "DockTitle"="독"; "MusicPlayerTitle"="플레이어";
        "SearchPlaceholder"="앱, 파일 등 검색…";
        "WidgetClock"="시계"; "WidgetWeather"="날씨"; "WidgetCalendar"="캘린더"; "WidgetMusic"="음악";
        "WidgetPomodoro"="집중 타이머"; "WidgetPhoto"="사진"; "WidgetAffirmation"="확언";
        "WidgetSystemStats"="시스템 상태"; "WidgetPixelArt"="픽셀 아트"; "WidgetShapePhoto"="모양 사진"; "WidgetGif"="GIF";
        "SpaceDefault"="기본 스페이스"; "SpaceNewName"="새 스페이스";
        "WorkflowTitle"="워크플로"; "WorkflowNew"="새 워크플로"; "WorkflowRun"="실행";
        "AgentApproveTitle"="에이전트 요청"; "AgentApprove"="허용"; "AgentDeny"="거부";
        "SettingsTitle"="설정"; "SettingsGeneral"="일반"; "SettingsAppearance"="모양"; "SettingsNotifications"="알림";
        "PhoneLinkTitle"="휴대폰과 연결"; "PhoneLinkPair"="새 기기 페어링"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="레이아웃";
        "CloseButton"="닫기"; "OkButton"="확인"; "CancelButton"="취소";
        "NotchClipboard"="클립보드"; "NotchPocket"="포켓 선반"; "NotchNearbyShare"="근거리 공유"; "NotchFocusMode"="집중 모드";
        "MusicModeVinyl"="바이닐"; "MusicModeFullArt"="전체 화면 앨범 아트"; "MusicModeParticles"="파티클"; "MusicModeGlowingBeats"="글로우 비트"
    }
    "ar-SA" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="المُشغّل"; "DockTitle"="الشريط السفلي"; "MusicPlayerTitle"="المشغّل";
        "SearchPlaceholder"="ابحث عن التطبيقات والملفات والمزيد…";
        "WidgetClock"="الساعة"; "WidgetWeather"="الطقس"; "WidgetCalendar"="التقويم"; "WidgetMusic"="الموسيقى";
        "WidgetPomodoro"="مؤقت التركيز"; "WidgetPhoto"="صورة"; "WidgetAffirmation"="تأكيد يومي";
        "WidgetSystemStats"="حالة النظام"; "WidgetPixelArt"="فن البكسل"; "WidgetShapePhoto"="صورة مُشكّلة"; "WidgetGif"="GIF";
        "SpaceDefault"="المساحة الافتراضية"; "SpaceNewName"="مساحة جديدة";
        "WorkflowTitle"="سير العمل"; "WorkflowNew"="سير عمل جديد"; "WorkflowRun"="تشغيل";
        "AgentApproveTitle"="طلب الوكيل"; "AgentApprove"="سماح"; "AgentDeny"="رفض";
        "SettingsTitle"="الإعدادات"; "SettingsGeneral"="عام"; "SettingsAppearance"="المظهر"; "SettingsNotifications"="الإشعارات";
        "PhoneLinkTitle"="ربط الهاتف"; "PhoneLinkPair"="إقران جهاز جديد"; "PhoneLinkPairPin"="الرمز: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="التخطيط";
        "CloseButton"="إغلاق"; "OkButton"="موافق"; "CancelButton"="إلغاء";
        "NotchClipboard"="الحافظة"; "NotchPocket"="رف Pocket"; "NotchNearbyShare"="المشاركة القريبة"; "NotchFocusMode"="وضع التركيز";
        "MusicModeVinyl"="أسطوانة فينيل"; "MusicModeFullArt"="غلاف ملء الشاشة"; "MusicModeParticles"="جزيئات"; "MusicModeGlowingBeats"="إيقاعات متوهجة"
    }
    "ru-RU" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Лаунчер"; "DockTitle"="Док"; "MusicPlayerTitle"="Плеер";
        "SearchPlaceholder"="Поиск приложений, файлов и прочего…";
        "WidgetClock"="Часы"; "WidgetWeather"="Погода"; "WidgetCalendar"="Календарь"; "WidgetMusic"="Музыка";
        "WidgetPomodoro"="Таймер фокуса"; "WidgetPhoto"="Фото"; "WidgetAffirmation"="Аффирмация";
        "WidgetSystemStats"="Монитор системы"; "WidgetPixelArt"="Пиксель-арт"; "WidgetShapePhoto"="Фигурное фото"; "WidgetGif"="GIF";
        "SpaceDefault"="Основное пространство"; "SpaceNewName"="Новое пространство";
        "WorkflowTitle"="Сценарии"; "WorkflowNew"="Новый сценарий"; "WorkflowRun"="Запустить";
        "AgentApproveTitle"="Запрос агента"; "AgentApprove"="Разрешить"; "AgentDeny"="Отклонить";
        "SettingsTitle"="Настройки"; "SettingsGeneral"="Основные"; "SettingsAppearance"="Оформление"; "SettingsNotifications"="Уведомления";
        "PhoneLinkTitle"="Связь с телефоном"; "PhoneLinkPair"="Подключить устройство"; "PhoneLinkPairPin"="ПИН-код: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Макет";
        "CloseButton"="Закрыть"; "OkButton"="ОК"; "CancelButton"="Отмена";
        "NotchClipboard"="Буфер обмена"; "NotchPocket"="Полка Pocket"; "NotchNearbyShare"="Обмен с устройствами"; "NotchFocusMode"="Режим фокусировки";
        "MusicModeVinyl"="Винил"; "MusicModeFullArt"="Обложка на весь экран"; "MusicModeParticles"="Частицы"; "MusicModeGlowingBeats"="Световые ритмы"
    }
    "hi-IN" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="लॉन्चर"; "DockTitle"="डॉक"; "MusicPlayerTitle"="प्लेयर";
        "SearchPlaceholder"="ऐप्स, फ़ाइलें और अधिक खोजें…";
        "WidgetClock"="घड़ी"; "WidgetWeather"="मौसम"; "WidgetCalendar"="कैलेंडर"; "WidgetMusic"="संगीत";
        "WidgetPomodoro"="फ़ोकस टाइमर"; "WidgetPhoto"="फ़ोटो"; "WidgetAffirmation"="सकारात्मक विचार";
        "WidgetSystemStats"="सिस्टम आँकड़े"; "WidgetPixelArt"="पिक्सेल आर्ट"; "WidgetShapePhoto"="आकार फ़ोटो"; "WidgetGif"="GIF";
        "SpaceDefault"="डिफ़ॉल्ट स्पेस"; "SpaceNewName"="नया स्पेस";
        "WorkflowTitle"="वर्कफ़्लो"; "WorkflowNew"="नया वर्कफ़्लो"; "WorkflowRun"="चलाएँ";
        "AgentApproveTitle"="एजेंट अनुरोध"; "AgentApprove"="अनुमति दें"; "AgentDeny"="अस्वीकार करें";
        "SettingsTitle"="सेटिंग्स"; "SettingsGeneral"="सामान्य"; "SettingsAppearance"="दिखावट"; "SettingsNotifications"="सूचनाएँ";
        "PhoneLinkTitle"="फ़ोन लिंक"; "PhoneLinkPair"="नया डिवाइस पेयर करें"; "PhoneLinkPairPin"="पिन: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="लेआउट";
        "CloseButton"="बंद करें"; "OkButton"="ठीक है"; "CancelButton"="रद्द करें";
        "NotchClipboard"="क्लिपबोर्ड"; "NotchPocket"="पॉकेट शेल्फ़"; "NotchNearbyShare"="नियरबाय शेयर"; "NotchFocusMode"="फ़ोकस मोड";
        "MusicModeVinyl"="विनाइल"; "MusicModeFullArt"="फ़ुल स्क्रीन आर्ट"; "MusicModeParticles"="कण प्रभाव"; "MusicModeGlowingBeats"="चमकती बीट्स"
    }
    "nl-NL" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Starter"; "DockTitle"="Dock"; "MusicPlayerTitle"="Speler";
        "SearchPlaceholder"="Zoek apps, bestanden en meer…";
        "WidgetClock"="Klok"; "WidgetWeather"="Weer"; "WidgetCalendar"="Kalender"; "WidgetMusic"="Muziek";
        "WidgetPomodoro"="Focustimer"; "WidgetPhoto"="Foto"; "WidgetAffirmation"="Affirmatie";
        "WidgetSystemStats"="Systeemstatistieken"; "WidgetPixelArt"="Pixelkunst"; "WidgetShapePhoto"="Vormfoto"; "WidgetGif"="GIF";
        "SpaceDefault"="Standaardruimte"; "SpaceNewName"="Nieuwe ruimte";
        "WorkflowTitle"="Werkstromen"; "WorkflowNew"="Nieuwe werkstroom"; "WorkflowRun"="Uitvoeren";
        "AgentApproveTitle"="Agentverzoek"; "AgentApprove"="Toestaan"; "AgentDeny"="Weigeren";
        "SettingsTitle"="Instellingen"; "SettingsGeneral"="Algemeen"; "SettingsAppearance"="Weergave"; "SettingsNotifications"="Meldingen";
        "PhoneLinkTitle"="Telefoonkoppeling"; "PhoneLinkPair"="Nieuw apparaat koppelen"; "PhoneLinkPairPin"="Pincode: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Indeling";
        "CloseButton"="Sluiten"; "OkButton"="OK"; "CancelButton"="Annuleren";
        "NotchClipboard"="Klembord"; "NotchPocket"="Pocket-plank"; "NotchNearbyShare"="Dichtbij delen"; "NotchFocusMode"="Focusmodus";
        "MusicModeVinyl"="Vinyl"; "MusicModeFullArt"="Volledig Schermhoes"; "MusicModeParticles"="Deeltjes"; "MusicModeGlowingBeats"="Gloeiende Beats"
    }
    "pl-PL" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Program uruchamiający"; "DockTitle"="Dok"; "MusicPlayerTitle"="Odtwarzacz";
        "SearchPlaceholder"="Szukaj aplikacji, plików i więcej…";
        "WidgetClock"="Zegar"; "WidgetWeather"="Pogoda"; "WidgetCalendar"="Kalendarz"; "WidgetMusic"="Muzyka";
        "WidgetPomodoro"="Minutnik skupienia"; "WidgetPhoto"="Zdjęcie"; "WidgetAffirmation"="Afirmacja";
        "WidgetSystemStats"="Statystyki systemu"; "WidgetPixelArt"="Sztuka pikseli"; "WidgetShapePhoto"="Zdjęcie w kształcie"; "WidgetGif"="GIF";
        "SpaceDefault"="Domyślna przestrzeń"; "SpaceNewName"="Nowa przestrzeń";
        "WorkflowTitle"="Przepływy pracy"; "WorkflowNew"="Nowy przepływ"; "WorkflowRun"="Uruchom";
        "AgentApproveTitle"="Żądanie agenta"; "AgentApprove"="Zezwól"; "AgentDeny"="Odmów";
        "SettingsTitle"="Ustawienia"; "SettingsGeneral"="Ogólne"; "SettingsAppearance"="Wygląd"; "SettingsNotifications"="Powiadomienia";
        "PhoneLinkTitle"="Łącze z telefonem"; "PhoneLinkPair"="Sparuj nowe urządzenie"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Układ";
        "CloseButton"="Zamknij"; "OkButton"="OK"; "CancelButton"="Anuluj";
        "NotchClipboard"="Schowek"; "NotchPocket"="Półka Pocket"; "NotchNearbyShare"="Udostępnianie w pobliżu"; "NotchFocusMode"="Tryb skupienia";
        "MusicModeVinyl"="Winyl"; "MusicModeFullArt"="Okładka pełnoekranowa"; "MusicModeParticles"="Cząsteczki"; "MusicModeGlowingBeats"="Świecące bity"
    }
    "tr-TR" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Başlatıcı"; "DockTitle"="Dock"; "MusicPlayerTitle"="Oynatıcı";
        "SearchPlaceholder"="Uygulamalar, dosyalar ve daha fazlasını ara…";
        "WidgetClock"="Saat"; "WidgetWeather"="Hava Durumu"; "WidgetCalendar"="Takvim"; "WidgetMusic"="Müzik";
        "WidgetPomodoro"="Odak Zamanlayıcı"; "WidgetPhoto"="Fotoğraf"; "WidgetAffirmation"="Olumlama";
        "WidgetSystemStats"="Sistem İstatistikleri"; "WidgetPixelArt"="Piksel Sanatı"; "WidgetShapePhoto"="Şekilli Fotoğraf"; "WidgetGif"="GIF";
        "SpaceDefault"="Varsayılan Alan"; "SpaceNewName"="Yeni Alan";
        "WorkflowTitle"="İş Akışları"; "WorkflowNew"="Yeni İş Akışı"; "WorkflowRun"="Çalıştır";
        "AgentApproveTitle"="Ajan İsteği"; "AgentApprove"="İzin Ver"; "AgentDeny"="Reddet";
        "SettingsTitle"="Ayarlar"; "SettingsGeneral"="Genel"; "SettingsAppearance"="Görünüm"; "SettingsNotifications"="Bildirimler";
        "PhoneLinkTitle"="Telefon Bağlantısı"; "PhoneLinkPair"="Yeni Cihaz Eşleştir"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Düzen";
        "CloseButton"="Kapat"; "OkButton"="Tamam"; "CancelButton"="İptal";
        "NotchClipboard"="Pano"; "NotchPocket"="Pocket Rafı"; "NotchNearbyShare"="Yakındakilerle Paylaş"; "NotchFocusMode"="Odak Modu";
        "MusicModeVinyl"="Plak"; "MusicModeFullArt"="Tam Ekran Kapak"; "MusicModeParticles"="Parçacıklar"; "MusicModeGlowingBeats"="Parlayan Ritimler"
    }
    "sv-SE" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Startare"; "DockTitle"="Docka"; "MusicPlayerTitle"="Spelare";
        "SearchPlaceholder"="Sök appar, filer och mer…";
        "WidgetClock"="Klocka"; "WidgetWeather"="Väder"; "WidgetCalendar"="Kalender"; "WidgetMusic"="Musik";
        "WidgetPomodoro"="Fokustimer"; "WidgetPhoto"="Foto"; "WidgetAffirmation"="Affirmation";
        "WidgetSystemStats"="Systemstatistik"; "WidgetPixelArt"="Pixelkonst"; "WidgetShapePhoto"="Formfoto"; "WidgetGif"="GIF";
        "SpaceDefault"="Standardutrymme"; "SpaceNewName"="Nytt utrymme";
        "WorkflowTitle"="Arbetsflöden"; "WorkflowNew"="Nytt flöde"; "WorkflowRun"="Kör";
        "AgentApproveTitle"="Agentbegäran"; "AgentApprove"="Tillåt"; "AgentDeny"="Neka";
        "SettingsTitle"="Inställningar"; "SettingsGeneral"="Allmänt"; "SettingsAppearance"="Utseende"; "SettingsNotifications"="Aviseringar";
        "PhoneLinkTitle"="Telefonlänk"; "PhoneLinkPair"="Parkoppla ny enhet"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Layout";
        "CloseButton"="Stäng"; "OkButton"="OK"; "CancelButton"="Avbryt";
        "NotchClipboard"="Urklipp"; "NotchPocket"="Pocket-hylla"; "NotchNearbyShare"="Dela i närheten"; "NotchFocusMode"="Fokusläge";
        "MusicModeVinyl"="Vinyl"; "MusicModeFullArt"="Helskärmsomslag"; "MusicModeParticles"="Partiklar"; "MusicModeGlowingBeats"="Glödande Beats"
    }
    "da-DK" = [ordered]@{
        "AppName"="LaunchMe"; "LauncherTitle"="Starter"; "DockTitle"="Dock"; "MusicPlayerTitle"="Afspiller";
        "SearchPlaceholder"="Søg efter apps, filer og mere…";
        "WidgetClock"="Ur"; "WidgetWeather"="Vejr"; "WidgetCalendar"="Kalender"; "WidgetMusic"="Musik";
        "WidgetPomodoro"="Fokustimer"; "WidgetPhoto"="Foto"; "WidgetAffirmation"="Bekræftelse";
        "WidgetSystemStats"="Systemstatistik"; "WidgetPixelArt"="Pixelkunst"; "WidgetShapePhoto"="Formfoto"; "WidgetGif"="GIF";
        "SpaceDefault"="Standardrum"; "SpaceNewName"="Nyt rum";
        "WorkflowTitle"="Arbejdsgange"; "WorkflowNew"="Ny arbejdsgang"; "WorkflowRun"="Kør";
        "AgentApproveTitle"="Agentanmodning"; "AgentApprove"="Tillad"; "AgentDeny"="Afvis";
        "SettingsTitle"="Indstillinger"; "SettingsGeneral"="Generelt"; "SettingsAppearance"="Udseende"; "SettingsNotifications"="Notifikationer";
        "PhoneLinkTitle"="Telefonlink"; "PhoneLinkPair"="Par ny enhed"; "PhoneLinkPairPin"="PIN: {0}";
        "HyprWinTitle"="HyprWin"; "HyprWinLayout"="Layout";
        "CloseButton"="Luk"; "OkButton"="OK"; "CancelButton"="Annuller";
        "NotchClipboard"="Udklipsholder"; "NotchPocket"="Pocket-hylde"; "NotchNearbyShare"="Del i nærheden"; "NotchFocusMode"="Fokustilstand";
        "MusicModeVinyl"="Vinyl"; "MusicModeFullArt"="Fuldskærmscover"; "MusicModeParticles"="Partikler"; "MusicModeGlowingBeats"="Glødende Beats"
    }
}

foreach ($culture in $translations.Keys) {
    $dir = Join-Path $baseDir $culture
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $path = Join-Path $dir "Resources.resw"

    $xml = [System.Xml.XmlDocument]::new()
    $xml.AppendChild($xml.CreateXmlDeclaration("1.0", "utf-8", $null)) | Out-Null
    $root = $xml.AppendChild($xml.CreateElement("root"))
    $root.AppendChild($xml.CreateComment(" Microsoft ResX Schema — LaunchMe-Win Localization ($culture) ")) | Out-Null

    $dict = $translations[$culture]
    foreach ($key in $dict.Keys) {
        $data = $root.AppendChild($xml.CreateElement("data"))
        $data.SetAttribute("name", $key)
        $data.SetAttribute("xml:space", "preserve")
        $value = $data.AppendChild($xml.CreateElement("value"))
        $value.InnerText = $dict[$key]
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $sw = New-Object System.IO.StreamWriter($path, $false, $utf8NoBom)
    $xml.Save($sw)
    $sw.Close()
}

Write-Host "Generated $($translations.Count) fully translated .resw files ($($translations['en-US'].Count) keys each)."

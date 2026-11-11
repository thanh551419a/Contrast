# HTML extraction

Source: CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html:25 window.__CE_LEVELS__. All campaign levels run configured EASY, not the built-in HARD preset (:3002-3005).

|ID|Name|Terrain|Checkpoints excluding spawn|Collectibles|World pixels|Spawn left/feet|
|---|---|---:|---:|---:|---|---|
|L01|Cánh cửa không phải vật cản|16|5|2|6169x1693|-571/972|
|L02|Cầu thang ma|15|4|2|6144x1562|110/940|
|L03|Giếng ba tầng|27|8|3|6235x3238|245/262|

Player38x64 pixels (CFG:91-103). Rectangles are top-left pixels, y downward. Unity center=((x+w/2)/64,-(y+h/2)/64); size=(w/64,h/64). Spawn center=((cp.x+19)/64,-(cp.feet-32)/64). L01 spawn=(-8.625,-14.6875), size=(.59375,1), floor center=(-6.6796875,-15.7265625).

HTML free world (:332-411,1765-1767) has unconstrained camera, dynamic void death560px below support with reachable-landing test (:1728-1763). deathY null; authored world dimensions do not define fixed death plane.

A/D movement; arrows phase2.75rad/s; Space/W jump; S fast fall. Ground/air acceleration2300/1600px/s², speed360, gravity1650, jump-820, maxfall1050, coyote.10s, buffer.12s. Release scales rising velocity*.56 when faster than180 (:630). Matching WHITE>=.78, BLACK<=.22, GRAY intermediate passes; FIXED blocks (:538-550). Ghost overlap persists until exit (:753-798).

Sequential checkpoints use expanded2px zones (:1009-1023); respawn restores separate x/feet/phase and clears velocity/ghost/trail (:1045-1055). Collectibles are optional for victory, persistent per-level IDs (:2897-2937).

Newton centers L01(5135,659)i3/k6; L02(5110,528)i2/k4; L03(5201,2100)i2/k7. L01 K(width136,period10.8,angle24.4,direction-1), I(126,8.6,135,+1); L02 K(150,11.2,192,-1),I(140,10.5,155,+1); L03 K(150,11.2,180,-1),I(140,10.5,155,+1). Heights22/20. Rails stay horizontal. Radius220*sqrt(n), angle=initial+direction*clock/period*2pi (:847-861,1095). L01/L03 auto; L02 docks until standing on K.

L03 three cosine movers: (dx,dy,period)=(0,-127,4),(213,-258,5),(461,676,5), phase0; ease=(1-cos(2pi*t))/2 (:872-887).

X uses held rising edge; active radius1012px; K grounded capture sets axis, I antipodal48px tolerance, K-prime54px; third capture reveals5.6s then completes (:1192-1242,1537-1541).

Full per-object authored extraction and raw preserved fields are also in ../HTML_LEVEL_EXTRACTION.md and ../json/JSON_PARITY_REPORT.md. Those prior artifacts were inspected; this report distinguishes fresh execution from prior claims.

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BacktestAnalyzer;

public static class HtmlExport
{
    public static void Save(Report report,string path,bool dark)
    {
        var events=new List<object[]>();decimal peak=report.InitialDeposit,buy=0,sell=0;
        if(report.Balances.Count>0&&report.Deals.Count>0&&report.Balances[0].Time<=report.Deals[0].Time)
        {
            var first=report.Balances[0];peak=Math.Max(peak,first.Balance);
            events.Add(new object[]{new DateTimeOffset(DateTime.SpecifyKind(first.Time,DateTimeKind.Utc)).ToUnixTimeMilliseconds(),first.Balance,peak-first.Balance,peak>0?(peak-first.Balance)/peak*100:0,0,0,"Startkapital",peak});
        }
        // Inventory volume is independent of an individual FIFO/P&L pairing.
        foreach(var deal in report.Deals)
        {
            if(deal.Entry=="in") {if(deal.Side=="buy")buy+=deal.Volume;else sell+=deal.Volume;}
            else if(deal.Entry is "out" or "outby" or "out/by") {if(deal.Side=="sell")buy-=deal.Volume;else sell-=deal.Volume;}
            else if(deal.Entry is "inout" or "in/out")
            {
                if(deal.Side=="sell"){var used=Math.Min(buy,deal.Volume);buy-=used;sell+=deal.Volume-used;}
                else {var used=Math.Min(sell,deal.Volume);sell-=used;buy+=deal.Volume-used;}
            }
            if(!deal.Balance.HasValue)continue;
            decimal balance=deal.Balance.Value;peak=Math.Max(peak,balance);decimal money=peak-balance,percent=peak>0?money/peak*100:0;
            events.Add(new object[]{new DateTimeOffset(DateTime.SpecifyKind(deal.Time,DateTimeKind.Utc)).ToUnixTimeMilliseconds(),balance,money,percent,buy,sell,deal.Id,peak});
        }
        if(events.Count<2)throw new InvalidDataException("Zu wenige numerische Balancewerte für einen interaktiven Verlauf.");
        string title=WebUtility.HtmlEncode(report.Strategy),currency=WebUtility.HtmlEncode(report.Find("Währung","Currency"));
        string logo=Convert.ToBase64String(PdfExport.Logo);string data=JsonSerializer.Serialize(events);
        string html=$$$$"""
<!doctype html>
<html lang="de"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Backtest-Analyzer – {{{{title}}}}</title>
<style>
*{box-sizing:border-box}p,.metric,header>div{min-width:0;overflow-wrap:anywhere}body{margin:0;background:#f2f5f9;color:#173047;font:15px system-ui,Segoe UI,sans-serif;--surface:#fff;--ink:#173047;--muted:#52677d;--line:#dce4ed;--blue:#174a79;--orange:#dc8b28}body.dark{background:#101b2b;color:#eaf1fa;--surface:#19273b;--ink:#eaf1fa;--muted:#a9b9ce;--line:#3b4d64;--blue:#76b7ff;--orange:#ffb348}main{max-width:1380px;margin:auto;padding:24px}header{display:flex;align-items:center;gap:18px;margin-bottom:22px}header img{width:76px;background:white;border-radius:10px}h1{font-size:28px;margin:0 0 4px}h2{font-size:21px;margin:0 0 5px}p{line-height:1.5;color:var(--muted)}button,select{padding:9px 13px;border:1px solid var(--line);background:var(--surface);color:var(--ink);border-radius:6px;cursor:pointer}.bar{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin:12px 0}.panel{background:var(--surface);padding:22px;border:1px solid var(--line);border-radius:12px}.details{display:grid;grid-template-columns:repeat(4,1fr);gap:10px;margin:14px 0}.metric{padding:14px;background:var(--surface);border:1px solid var(--line);border-radius:8px}.metric small{display:block;color:var(--muted);font-size:12px;margin-bottom:6px}.metric strong{font-size:19px}canvas{width:100%;height:510px;display:block;touch-action:pan-y;outline:none}canvas:focus{outline:2px solid var(--blue)}footer{font-size:12px;display:flex;justify-content:space-between;gap:12px;flex-wrap:wrap;padding:20px 0;color:var(--muted)}a{color:var(--blue)}.notice{border-left:4px solid var(--orange);padding:10px 14px;background:var(--surface);font-size:13px}.muted{font-size:12px;color:var(--muted)}@media(max-width:800px){main{padding:12px}.panel{padding:12px}.details{grid-template-columns:repeat(2,1fr)}canvas{height:420px}h1{font-size:23px}}
</style></head><body class="{{{{(dark?"dark":"")}}}}"><main>
<header><img src="data:image/png;base64,{{{{logo}}}}" alt="Backtest-Analyzer Logo"><div><h1>Backtest-Analyzer</h1><div>{{{{title}}}} · {{{{report.Platform}}}} · Interaktiver Report · BETA 0.2</div></div></header>
<div class="notice">Offline-Datei mit eingebetteten Daten. Balance und Drawdown stammen aus der exportierten Deal-History. Equity-Zeitreihe und bestätigte Positionsanzahl sind in dieser Datei nicht enthalten. Die offenen Lots sind Handelsvolumen, keine Marginbelastung.</div>
<div class="bar"><button id="theme">Light / Dark</button><label>Zeitraum <select id="month"><option value="all">Gesamter Test</option></select></label><button id="reset">Gesamten Verlauf anzeigen</button><span class="muted">Maus oder Touch für Zeitcursor · Pfeiltasten für einzelne Deals · Ctrl + Mausrad zum Zoomen</span></div>
<div class="details">
<div class="metric"><small>Beobachteter Zeitpunkt (Broker-Zeit)</small><strong id="time">–</strong></div><div class="metric"><small>Balance ({{{{currency}}}})</small><strong id="balance">–</strong></div><div class="metric"><small>Drawdown vom bisherigen Hoch</small><strong id="money">–</strong></div><div class="metric"><small>Drawdown (%)</small><strong id="percent">–</strong></div>
<div class="metric"><small>Offene Buy-Lots</small><strong id="buy">–</strong></div><div class="metric"><small>Offene Sell-Lots</small><strong id="sell">–</strong></div><div class="metric"><small>Netto-Lots (+ Long / − Short)</small><strong id="net">–</strong></div><div class="metric"><small>Höchststand ({{{{currency}}}})</small><strong id="peak">–</strong></div>
</div>
<section class="panel"><h2>Balance und dauerhafter Drawdown</h2><div id="event" class="muted"></div><canvas id="chart" tabindex="0" role="img" aria-label="Interaktive Balance- und Drawdownkurve. Mit Pfeiltasten Deal für Deal untersuchen."></canvas><div class="bar"><button id="previous">Vorheriger Deal</button><button id="next">Nächster Deal</button><button id="maxpercent">Maximaler Prozent-DD</button><button id="maxmoney">Maximaler Geld-DD</button></div></section>
<p class="muted">Gezeigt werden reale exportierte Beobachtungen, keine interpolierten Equitywerte. Mehrere Buchungen können denselben sekundengenauen Zeitstempel haben; dann lassen sich ihre Balancewerte mit den Deal-Tasten einzeln untersuchen. Kontobewegungen zwischen Handelsdeals und fehlende Zeilen können die Kurve beeinflussen. Quellenhash: {{{{report.Hash}}}}</p>
<footer><span>© {{{{DateTime.Now.Year}}}} Michael P. Thiess · BETA · Fehler möglich · Unabhängig von MetaQuotes</span><a href="{{{{PdfExport.Repository}}}}" target="_blank" rel="noopener">GitHub: Backtest-Analyzer</a></footer></main>
<script>
'use strict';const rows={{{{data}}}},canvas=document.getElementById('chart'),ctx=canvas.getContext('2d'),moneyFormat=new Intl.NumberFormat('de-DE',{maximumFractionDigits:2,minimumFractionDigits:2}),countFormat=new Intl.NumberFormat('de-DE',{maximumFractionDigits:2});let selected=rows.length-1,start=rows[0][0],end=rows.at(-1)[0],width=1000,height=510,rangeStart=0,rangeEnd=rows.length-1;
const el=id=>document.getElementById(id),fmt=v=>moneyFormat.format(v),time=v=>new Date(v).toISOString().replace('T',' ').slice(0,19),css=name=>getComputedStyle(document.body).getPropertyValue(name).trim();
function search(t){let lo=0,hi=rows.length-1;while(lo<hi){const mid=(lo+hi)>>1;if(rows[mid][0]<t)lo=mid+1;else hi=mid}return lo}
const months=[...new Set(rows.map(r=>new Date(r[0]).toISOString().slice(0,7)))];for(const month of months){const option=document.createElement('option');option.value=month;option.textContent=month;el('month').append(option)}
function info(){const r=rows[selected];el('time').textContent=time(r[0]);el('balance').textContent=fmt(r[1]);el('money').textContent=fmt(r[2]);el('percent').textContent=fmt(r[3])+' %';el('buy').textContent=countFormat.format(r[4]);el('sell').textContent=countFormat.format(r[5]);el('net').textContent=countFormat.format(r[4]-r[5]);el('peak').textContent=fmt(r[7]);el('event').textContent='Deal '+r[6]+' · Beobachtung '+(selected+1).toLocaleString('de-DE')+' / '+rows.length.toLocaleString('de-DE')+' · Equity: nicht exportiert · Positionsanzahl: ohne Positionsbezüge nicht gesichert';canvas.setAttribute('aria-label','Deal '+r[6]+', '+time(r[0])+', Balance '+fmt(r[1])+', Drawdown '+fmt(r[3])+' Prozent')}
function draw(){width=canvas.clientWidth;height=canvas.clientHeight;const scale=devicePixelRatio||1;canvas.width=width*scale;canvas.height=height*scale;ctx.setTransform(scale,0,0,scale,0,0);ctx.clearRect(0,0,width,height);const left=100,right=width-18,top=24,bottom=height-38,split=height*.65,plot=right-left,span=Math.max(1,end-start);rangeStart=Math.max(0,search(start)-1);rangeEnd=search(end);let minimum=Infinity,maximum=-Infinity,dd=0;for(let i=rangeStart;i<=rangeEnd;i++){minimum=Math.min(minimum,rows[i][1]);maximum=Math.max(maximum,rows[i][1]);dd=Math.max(dd,rows[i][3])}let padding=Math.max(1,(maximum-minimum)*.07);minimum-=padding;maximum+=padding;dd=Math.max(10,Math.ceil(dd/10)*10);const x=t=>left+(t-start)/span*plot,y=v=>split-20-(v-minimum)/(maximum-minimum)*(split-top-20),dy=v=>split+18+v/dd*(bottom-split-18);ctx.font='12px system-ui';ctx.fillStyle=css('--muted');ctx.strokeStyle=css('--line');ctx.lineWidth=1;
for(let i=0;i<=5;i++){const v=minimum+(maximum-minimum)*i/5,yy=y(v);ctx.beginPath();ctx.moveTo(left,yy);ctx.lineTo(right,yy);ctx.stroke();ctx.fillText(countFormat.format(v),4,yy+4)}for(let i=0;i<=3;i++){const v=dd*i/3,yy=dy(v);ctx.beginPath();ctx.moveTo(left,yy);ctx.lineTo(right,yy);ctx.stroke();ctx.fillText('−'+countFormat.format(v)+' %',24,yy+4)}
// Preserve balance and DD extrema in each rendering bucket. Full rows remain available to the cursor.
const keep=new Set([rangeStart,rangeEnd]);let stride=Math.max(1,Math.floor((rangeEnd-rangeStart)/plot));for(let i=rangeStart;i<=rangeEnd;i+=stride){let min=i,max=i,worst=i;for(let j=i;j<=Math.min(rangeEnd,i+stride-1);j++){if(rows[j][1]<rows[min][1])min=j;if(rows[j][1]>rows[max][1])max=j;if(rows[j][3]>rows[worst][3])worst=j}keep.add(min);keep.add(max);keep.add(worst)}const points=[...keep].sort((a,b)=>a-b);ctx.save();ctx.beginPath();ctx.rect(left,top,plot,bottom-top);ctx.clip();
function curve(value,position,color,base){ctx.beginPath();ctx.moveTo(x(rows[points[0]][0]),base);for(const i of points)ctx.lineTo(x(rows[i][0]),position(rows[i][value]));ctx.lineTo(x(rows[points.at(-1)][0]),base);ctx.closePath();const grad=ctx.createLinearGradient(0,top,0,bottom);grad.addColorStop(0,color+'55');grad.addColorStop(1,color+'08');ctx.fillStyle=grad;ctx.fill();ctx.beginPath();points.forEach((i,n)=>{if(!n)ctx.moveTo(x(rows[i][0]),position(rows[i][value]));else ctx.lineTo(x(rows[i][0]),position(rows[i][value]))});ctx.strokeStyle=color;ctx.lineWidth=1.3;ctx.stroke()}
curve(1,y,css('--blue'),split-20);curve(3,dy,css('--orange'),split+18);const r=rows[selected],xx=x(r[0]);if(r[0]>=start&&r[0]<=end){ctx.strokeStyle=css('--muted');ctx.setLineDash([4,4]);ctx.beginPath();ctx.moveTo(xx,top);ctx.lineTo(xx,bottom);ctx.stroke();ctx.setLineDash([]);ctx.fillStyle=css('--blue');ctx.beginPath();ctx.arc(xx,y(r[1]),4,0,Math.PI*2);ctx.fill();ctx.fillStyle=css('--orange');ctx.beginPath();ctx.arc(xx,dy(r[3]),4,0,Math.PI*2);ctx.fill()}ctx.restore();ctx.fillStyle=css('--muted');const ticks=Math.max(1,Math.min(5,Math.floor(plot/140)));for(let i=0;i<=ticks;i++){const t=start+span*i/ticks,label=new Date(t).toISOString().slice(0,10),labelWidth=ctx.measureText(label).width,xx=Math.min(width-labelWidth-5,Math.max(5,left+plot*i/ticks-labelWidth/2));ctx.fillText(label,xx,bottom+24)}ctx.fillText('Balance',left,13);ctx.fillText('Drawdown vom bisherigen Hoch',left,split+10);info()}
function choose(i){selected=Math.min(rows.length-1,Math.max(0,i));if(rows[selected][0]<start||rows[selected][0]>end){start=rows[0][0];end=rows.at(-1)[0];el('month').value='all'}draw()}
canvas.addEventListener('pointermove',event=>{const rect=canvas.getBoundingClientRect(),ratio=Math.max(0,Math.min(1,(event.clientX-rect.left-100)/(canvas.clientWidth-118)));selected=Math.min(rows.length-1,search(start+(end-start)*ratio));draw()});canvas.addEventListener('keydown',event=>{if(event.key==='ArrowLeft'||event.key==='ArrowRight'){event.preventDefault();choose(selected+(event.key==='ArrowLeft'?-1:1))}});
canvas.addEventListener('wheel',event=>{if(!event.ctrlKey&&!event.shiftKey)return;event.preventDefault();const span=(end-start)*(event.deltaY>0?1.25:.8),center=rows[selected][0];start=Math.max(rows[0][0],center-span/2);end=Math.min(rows.at(-1)[0],center+span/2);el('month').value='all';draw()},{passive:false});
el('previous').onclick=()=>choose(selected-1);el('next').onclick=()=>choose(selected+1);el('theme').onclick=()=>{document.body.classList.toggle('dark');draw()};el('reset').onclick=()=>{start=rows[0][0];end=rows.at(-1)[0];el('month').value='all';draw()};el('month').onchange=()=>{if(el('month').value==='all'){el('reset').click();return}start=Date.parse(el('month').value+'-01T00:00:00Z');const date=new Date(start);date.setUTCMonth(date.getUTCMonth()+1);end=date.getTime()-1;selected=search(start);draw()};el('maxpercent').onclick=()=>choose(rows.reduce((best,r,i)=>r[3]>rows[best][3]?i:best,0));el('maxmoney').onclick=()=>choose(rows.reduce((best,r,i)=>r[2]>rows[best][2]?i:best,0));new ResizeObserver(draw).observe(canvas);draw();
</script></body></html>
""";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);File.WriteAllText(path,html,new UTF8Encoding(false));
    }
}

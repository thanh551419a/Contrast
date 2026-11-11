const fs=require('fs'),path=require('path'),rl=require('readline');
const root=process.cwd(),out=path.join(root,'Reports/GameplayParity/Run_20261102');
async function records(file,keep){const a=[];for await(const line of rl.createInterface({input:fs.createReadStream(file),crlfDelay:Infinity})){if(line){const r=JSON.parse(line);if(keep(r))a.push(r);}}return a;}
const norm=r=>({x:(r.position.x+r.size.x/2)/64,y:-(r.position.y+r.size.y/2)/64});
function interp(a,t){let lo=0,hi=a.length-1;while(lo<hi){const m=Math.floor((lo+hi)/2);if(a[m].t<t)lo=m+1;else hi=m;}const b=a[lo],p=a[Math.max(0,lo-1)];const k=b.t===p.t?0:(t-p.t)/(b.t-p.t);return {x:p.x+(b.x-p.x)*k,y:p.y+(b.y-p.y)*k};}
const mag=v=>Math.hypot(v.x,v.y),sub=(a,b)=>({x:a.x-b.x,y:a.y-b.y});
(async()=>{const results=[];for(const s of ['idle','horizontal','jump','combined','phase','x']){
 const h=await records(path.join(root,'Reports/GameplayParity/GAMEPLAY_PARITY_LOGS/HTML-L01-'+(s==='x'?'x_before_activation':s)+'.jsonl'),r=>r.event==='frame'&&r.objectId==='player');
 const u=await records(path.join(out,'Unity-approved-'+s+'.jsonl'),r=>r.eventName==='object_sample'&&r.objectId==='player');
 const input=await records(path.join(out,'Unity-approved-'+s+'-input.jsonl'),()=>true),byFrame=new Map(input.map(r=>[r.frameIndex,r]));
 const ha=h.map(r=>({t:r.simulationTime,...norm(r)})),ua=u.filter(r=>byFrame.has(r.frameIndex)).map(r=>({t:byFrame.get(r.frameIndex).scenarioTime,...r.position}));
 const end=Math.min(ha.at(-1).t,ua.at(-1).t),rows=[],errs=[],abs=[];let agree=0,meaningful=0;const step=.05,dead=.01;
 for(let t=ha[0].t+step;t<=end;t+=step){const hp=interp(ha,t-step),hn=interp(ha,t),up=interp(ua,t-step),un=interp(ua,t),hd=sub(hn,hp),ud=sub(un,up),e=mag(sub(ud,hd)),n=e/Math.max(mag(hd),dead);if(Math.max(mag(hd),mag(ud))>dead){meaningful++;if(hd.x*ud.x+hd.y*ud.y>0)agree++;errs.push(n);}abs.push(e);rows.push([t,hn.x,hn.y,un.x,un.y,hd.x,hd.y,ud.x,ud.y,e,n]);}
 errs.sort((a,b)=>a-b);const q=p=>errs.length?errs[Math.floor((errs.length-1)*p)]:0;
 const result={scenario:s,htmlSamples:h.length,unitySamples:u.length,comparedUntil:end,initialAbsoluteError:mag(sub(ua[0],ha[0])),directionAgreement:meaningful?agree/meaningful:null,medianNormalizedError:q(.5),p95NormalizedError:q(.95),meanAbsoluteDeltaError:abs.reduce((a,b)=>a+b,0)/abs.length,unityTimelineCompleted:input.at(-1).scenarioTime>=3.99,status:(!meaningful?Math.max(...abs)<=dead:agree/meaningful>=.95&&q(.5)<=.2&&q(.95)<=.35)&&end>=3.99?'PASS':'FAIL',eventOrder:'NOT_VERIFIED',notes:s==='phase'?'Unity exited early; do not extrapolate.':s==='jump'||s==='combined'?'Jump release is unsupported by Unity adapter.':''};results.push(result);
 fs.writeFileSync(path.join(out,'aligned-approved-'+s+'.csv'),'time,html_x,html_y,unity_x,unity_y,html_dx,html_dy,unity_dx,unity_dy,absolute_delta_error,normalized_error\n'+rows.map(r=>r.join(',')).join('\n')+'\n');}
 fs.writeFileSync(path.join(out,'comparison-approved.json'),JSON.stringify({method:'Fixed geometry conversion64px/unit; centers; align Unity by actual adapter scenarioTime/frameIndex; linear interpolation at50ms; dead zone0.01 units per50ms; no trace fitting. Direction uses positive dot product; idle uses absolute threshold. PASS is movement metric only, event order not verified.',results},null,2));console.log(JSON.stringify(results,null,2));})();

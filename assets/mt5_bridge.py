"""Read-only MT5 candle bridge. No login, orders, or symbol modification calls."""
import sys,json
from datetime import datetime,timezone
import MetaTrader5 as mt

def main():
    action,path=sys.argv[1:3]
    if not mt.initialize(path=path,timeout=15000):raise RuntimeError(str(mt.last_error()))
    try:
        if action=='symbols':
            symbols=mt.symbols_get()
            if symbols is None:raise RuntimeError(str(mt.last_error()))
            print(json.dumps([{'name':s.name,'custom':bool(s.custom),'description':s.description,'currencyBase':s.currency_base,'currencyProfit':s.currency_profit,'contractSize':s.trade_contract_size} for s in symbols]));return
        if action=='quotes':
            name=sys.argv[3]
            if mt.symbol_info(name) is None:raise RuntimeError('Exact symbol not found: '+name)
            result=[]
            for event in sorted(set(json.loads(sys.argv[4]))):
                for minutes,frame in [(1,mt.TIMEFRAME_M1),(5,mt.TIMEFRAME_M5)]:
                    # Read a bounded local window; never download a complete M1 series.
                    bars=mt.copy_rates_range(name,frame,datetime.fromtimestamp(event-minutes*180,timezone.utc),datetime.fromtimestamp(event,timezone.utc))
                    usable=[] if bars is None else [b for b in bars if int(b['time'])+minutes*60<=event and event-(int(b['time'])+minutes*60)<minutes*60]
                    if usable:
                        b=max(usable,key=lambda b:int(b['time']))
                        result.append({'EventTime':datetime.fromtimestamp(event,timezone.utc).strftime('%Y-%m-%dT%H:%M:%S'),'CloseTime':datetime.fromtimestamp(int(b['time'])+minutes*60,timezone.utc).strftime('%Y-%m-%dT%H:%M:%S'),'Price':float(b['close']),'Minutes':minutes,'Symbol':name,'Source':'MT5 broker candle close'})
                        break
            print(json.dumps(result));return
        if action!='bars':raise ValueError('Unknown action')
        name=sys.argv[3];info=mt.symbol_info(name)
        if info is None:raise RuntimeError('Exact symbol not found: '+name)
        start,end=map(int,sys.argv[4:6]);bars=mt.copy_rates_range(name,mt.TIMEFRAME_H1,datetime.fromtimestamp(start,timezone.utc),datetime.fromtimestamp(end,timezone.utc))
        if bars is None or len(bars)==0:raise RuntimeError('No H1 history for exact symbol: '+name+'; '+str(mt.last_error()))
        print(json.dumps({'symbol':info.name,'custom':bool(info.custom),'bars':[{'Utc':datetime.fromtimestamp(int(b['time']),timezone.utc).strftime('%Y-%m-%dT%H:%M:%S'),'Open':float(b['open']),'High':float(b['high']),'Low':float(b['low']),'Close':float(b['close'])} for b in bars]}))
    finally:mt.shutdown()

try:main()
except Exception as e:print(str(e),file=sys.stderr);sys.exit(1)

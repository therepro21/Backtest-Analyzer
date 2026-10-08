# Project requirements

- Apply shared presentation changes to both the app and PDF workflows.
- Use a consistent typographic layout: matching outer left/right edges for headers, cards and chart containers; consistent internal insets, columns and spacing. Audit the complete layout in Light and Dark before delivery.
- Text next to the left edge of a plot is right aligned; text next to its right edge is left aligned, with a small consistent gap. Labels below bars or heatmap cells are centered on their category.
- Axis steps must be round, evenly spaced values suited to the instrument. Account money always carries the actual backtest account-currency symbol; USD is `$`, not `US$`. Instrument prices use the instrument's own price currency.
- Grid lines must remain visible inside filled areas in both themes. Paint fills, then grids, then curves/candles; retain readable line detail. Dark fills should be light and restrained.
- Keep complete year blocks and their sums together; two full years should fit per monthly PDF page without overly tight rows. Keep duration/date/price details inside equal-height metric cards.
- Default drawdown-event count is 10, with an optional 1–20 selection. Display 1–5 on the left and 6–10 on the right, and put trading duration first in the event details. Keep ranking numbers consistent with plot markers.
- Omit unavailable analyses from PDFs and repack the remaining panels. Explain missing prerequisites in the app. Never present invented estimates as exact observations.
- Basket MAE/MFE currently uses original stored tester-equity observations; disclose sampling and gaps. Do not automatically import M1 history unless the user requests it again.
- Analysis data must always be larger and more prominent than explanatory text. DD details need readable type and generous leading; use available space within blocks.
- Put technical explanations and formulas in a final two-column, small-type Help and explanation appendix. Important qualifications use compact numbered links at the relevant result. Do not add unsolicited technical prose to analysis panels.
- Fill usable pages proportionately: increase monthly row spacing and year separation while keeping two complete years on one page.
- Never clip headings; insert a blank paragraph before section headings and keep headings with following content.
- Counts have no decimal places. Money has two decimals and its currency; durations have explicit d/h/min/s units. Use locale-appropriate separators throughout app, reports and tooltips.
- Mark the final forced cycle/order start, last regular closure and test end visibly on all affected chronological charts. Distinguish forced-close performance from regular performance without deleting actual losses.
- Color the meaningful legend term itself instead of writing color names. Use established financial terminology and consistent German/English equivalents.
- Support account history as well as backtests. Preserve and show deposits, withdrawals, credit/bonus and other account bookings separately from trading P/L; adjust performance and drawdown calculations for external cash flows.

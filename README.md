# Rewind

Ett partyspel där spelarna tävlar om att välja den bästa låten till en prompt. TV:n visar spelet och spelar musiken, och mobilerna är handkontroller.

## Så spelar man

3–8 spelare går med genom att skanna QR-koden på TV:n. Varje runda har fyra faser:

1. **Inskick (45 s).** TV:n visar en prompt, till exempel "Låten som spelas när du inser att du gått på fel tåg". Alla söker fram en låt i mobilen och skickar in den. Ett inskickat bidrag är låst. Har någon redan valt samma låt får man välja en annan.
2. **Uppspelning.** TV:n spelar ett klipp av varje bidrag i slumpad ordning, utan att visa vem som valde låten.
3. **Röstning (20 s).** Alla röstar på sin favorit, men inte på sin egen.
4. **Avslöjande.** TV:n visar vem som valde vad. Varje röst ger 1 poäng.

Faserna går vidare så fort alla är klara. Kommer färre än två bidrag in hoppas rundan över.

I lobbyn väljer värden antalet rundor (3, 5 eller 8) och klipplängden (10, 15 eller 20 s). Efter sista rundan visas slutställningen, och "Spela igen" startar ett nytt spel med samma spelare.

## Struktur

```text
rewind/
├── backend/    .NET 10, Clean Architecture, Minimal API och SignalR
└── frontend/   React, TypeScript, Vite, Tailwind och Zustand
```

Musiken och klippen kommer från Deezers öppna API, som inte kräver någon nyckel.

## Kom igång

Fylls på när backend- och frontendskeletten finns.

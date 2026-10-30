# Mixtape

En samling musikspel för en kväll med kompisar. Storskärmen visar spelet och spelar musiken, och mobilerna är handkontroller.

## Aux Cord Battles

Det första spelläget. Spelarna tävlar om att välja den bästa låten till en prompt.

3–8 spelare går med genom att skanna QR-koden på storskärmen. Varje runda har fyra faser:

1. **Inskick (60–120 s).** Storskärmen visar en prompt, till exempel "Låten som spelas när du inser att du gått på fel tåg". Alla söker fram en låt i mobilen och skickar in den. Ett inskickat bidrag är låst. Har någon redan valt samma låt får man välja en annan.
2. **Uppspelning.** Storskärmen spelar ett klipp av varje bidrag i slumpad ordning och visar omslag, titel och artist, men inte vem som valde låten.
3. **Röstning (30 s).** Alla röstar på sin favorit, men inte på sin egen. Den som inte skickade in får också rösta.
4. **Avslöjande.** Storskärmen visar vem som valde vad. Varje röst ger 1 poäng.

Faserna går vidare så fort alla är klara. Kommer färre än två bidrag in hoppas rundan över.

I lobbyn väljer värden först spelläge. Drop the Needle är ett musikquiz för 2–8 spelare: storskärmen spelar ett klipp ur den valda kategorin, och alla gissar låten genom att söka fram den i mobilen. Poängen och avslöjandet efter varje låt byggs i #66. För Aux Cord Battles väljer värden sedan antalet rundor (3, 5 eller 8, standard 5), klipplängden (10, 15 eller 20 s, standard 15) och betänketiden i inskicket (60, 90 eller 120 s, standard 90). Efter sista rundan visas slutställningen, och "Spela igen" startar ett nytt spel med samma spelare i samma rum.

## Struktur

```text
mixtape/
├── backend/    .NET 10, Clean Architecture, Minimal API och SignalR
└── frontend/   React, TypeScript, Vite, Tailwind och Zustand
```

Musiken och klippen kommer från Deezers öppna API, som inte kräver någon nyckel.

## Kom igång

### Backend

Kräver .NET SDK 10.0.401 eller en nyare 10.0-version, enligt `global.json`.

```bash
cd backend
dotnet run --project src/Mixtape.Api
```

API:et svarar på `http://localhost:5080`, och `/health` visar att det är igång. Testerna körs med `dotnet test` i samma mapp.

### Frontend

Kräver Node i versionen i `frontend/.nvmrc`, och yarn classic (1.x).

```bash
cd frontend
yarn install
yarn dev
```

Vite skriver ut två adresser. `Local` (`http://localhost:5173`) öppnas på laptopen, och `Network` (till exempel `http://192.168.x.x:5173`) öppnas i mobilen på samma wifi. Backenden måste köra samtidigt, eftersom Vite skickar vidare `/gamehub` till den.

Kommer mobilen inte åt sidan: första gången Node lyssnar på nätverket frågar Windows-brandväggen om åtkomst. Tillåt den för privata nätverk. Har frågan redan avvisats, eller är wifi-nätverket inställt som offentligt, ändras det under Windows-inställningarna för nätverk och brandvägg.

### Före en PR

Kör samma kontroller som CI, för både backend och frontend. De står i [.github/workflows/ci.yml](.github/workflows/ci.yml).

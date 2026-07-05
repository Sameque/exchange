import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Asset } from '../models/asset.model';

@Injectable({
  providedIn: 'root'
})
export class AssetService {
  private mockAssets: Asset[] = [
    { symbol: 'PETR4', name: 'Petrobras' },
    { symbol: 'VALE3', name: 'Vale' },
    { symbol: 'VIIA4', name: 'Via' }
  ];

  getAssets(): Observable<Asset[]> {
    return of(this.mockAssets);
  }
}

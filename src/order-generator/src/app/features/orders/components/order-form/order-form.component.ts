import { Component, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { OrderService } from '../../../../core/services/order.service';
import { AssetService } from '../../../../core/services/asset.service';
import { Order } from '../../../../core/models/order.model';
import { Asset } from '../../../../core/models/asset.model';

@Component({
  selector: 'app-order-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './order-form.component.html',
  styleUrls: ['./order-form.component.css']
})
export class OrderFormComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  isLoading = signal(false);
  feedbackMessage = signal<{ text: string; type: 'success' | 'error' } | null>(null);
  assets = signal<Asset[]>([]);

  orderForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private orderService: OrderService,
    private assetService: AssetService
  ) {
    this.orderForm = this.fb.group({
      ticker: ['', [Validators.required]],
      side: ['BUY', [Validators.required]],
      quantity: [null, [
        Validators.required,
        Validators.min(1),
        Validators.max(99999),
        Validators.pattern('^[0-9]*$')
      ]],
      price: [null, [
        Validators.required,
        Validators.min(0.01),
        Validators.max(999.99),
        this.priceStepValidator(0.01)
      ]]
    });
  }

  ngOnInit(): void {
    this.loadAssets();
  }

  private loadAssets(): void {
    this.assetService.getAssets()
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => this.assets.set(data));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private priceStepValidator(step: number) {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;
      const value = Math.round(parseFloat(control.value) * 100) / 100;
      const remainder = value % step;
      const isStepValid = Math.abs(remainder) < 0.000001 || Math.abs(step - remainder) < 0.000001;
      return isStepValid ? null : { step: { value: step } };
    };
  }

  submitOrder() {
    if (this.orderForm.invalid) {
      this.orderForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.feedbackMessage.set(null);

    const orderData: Order = this.orderForm.value;

    this.orderService.sendOrder(orderData).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.status === 'Accepted') {
          this.feedbackMessage.set({ text: 'Ordem Aceita', type: 'success' });
          this.orderForm.reset({ side: 'BUY' });
        } else {
          this.feedbackMessage.set({
            text: `Ordem Rejeitada: ${res.rejectReason || 'Erro desconhecido'}`,
            type: 'error'
          });
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.feedbackMessage.set({
          text: `Erro na comunicação: ${err.message || 'Servidor indisponível'}`,
          type: 'error'
        });
      }
    });
  }
}

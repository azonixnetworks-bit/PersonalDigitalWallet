import { ChangeDetectionStrategy, Component, input } from '@angular/core';
@Component({selector:'app-ui-loader',standalone:true,templateUrl:'./ui-loader.component.html',styleUrl:'./ui-loader.component.css',changeDetection:ChangeDetectionStrategy.OnPush})
export class UiLoaderComponent { readonly label=input('Loading…'); readonly compact=input(false); }

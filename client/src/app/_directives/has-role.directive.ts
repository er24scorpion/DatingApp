import { User } from './../models/user';
import { take } from 'rxjs/operators';
import { AccountService } from './../_services/account.service';
import {
  Directive,
  Input,
  TemplateRef,
  ViewContainerRef,
  OnInit,
} from '@angular/core';

@Directive({
  selector: '[appHasRole]', // *appHasRole
})
export class HasRoleDirective implements OnInit {
  @Input() appHasRole: string[] = [];
  user!: User;

  constructor(
    private viewContainerRef: ViewContainerRef,
    private templateRef: TemplateRef<any>,
    private accountService: AccountService
  ) {
    this.accountService.currentUser$
      .pipe(take(1))
      .subscribe((user) => (this.user = user));
  }

  ngOnInit(): void {
    if (!this.user?.roles || this.user === null) {
      this.viewContainerRef.clear();
      return;
    }

    if (this.user?.roles.some((x) => this.appHasRole.includes(x))) {
      this.viewContainerRef.createEmbeddedView(this.templateRef);
    } else {
      this.viewContainerRef.clear();
    }
  }
}

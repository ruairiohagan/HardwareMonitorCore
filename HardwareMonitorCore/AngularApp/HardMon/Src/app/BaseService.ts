import { environment } from 'src/environments/environment';
import { HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';

export class BaseService {
    protected httpOptions = {
        headers: new HttpHeaders({ 'Content-Type': 'application/json' })
    };

    protected baseUrl: string = environment.apiUrl;

    protected errorHandlerRX<T>(operation = 'operation', result?: T) {
        return (error: any): Observable<T> => {
            var msg: string = error.message;
            if (error.error && error.error.ExceptionMessage) {
                msg = error.error.ExceptionMessage
            }
            // Just eat the error for now
            //alert('The following error occurred during ' + operation + '\n\n' + msg);

            // Let the app keep running by returning an empty result.
            return of(result as T);
        };
    }
}

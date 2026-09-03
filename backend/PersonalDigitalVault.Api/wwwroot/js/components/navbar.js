// Import token manager
// import { tokenManager } from '../auth/tokenManager.js';

// /**
//  * Initialize the navigation bar
//  * - Attaches logout handlers
//  * - Clears tokens and redirects to login
//  */
// export function initNavbar() {
//     const logoutButtons = document.querySelectorAll('[data-logout]');

//     logoutButtons.forEach(button => {
//         button.addEventListener('click', () => {
//             Clear authentication tokens
//             tokenManager.clear();

//             Redirect to login page
//             location.href = '/html/login.html';
//         });
//     });
// }
// button.addEventListener('click', () => {
//     if (confirm('Are you sure you want to log out?')) {
//         tokenManager.clear();
//         location.href = '/html/login.html';
//     }
// });


import {
    tokenManager
} from '../auth/tokenManager.js';


export function initNavbar() {
    // Event delegation use pannrom.
    // User navbar / Admin navbar rendu logout button-kum work aagum.
    document.addEventListener(
        'click',
        event => {
            const logoutButton =
                event.target.closest(
                    '[data-logout]'
                );


            if (!logoutButton) {
                return;
            }


            event.preventDefault();


            // Previous JWT token remove
            tokenManager.clear();


            // Session storage-la auth-related
            // temporary data irundhaalum clean pannrom
            sessionStorage.removeItem('pdv_token');
            sessionStorage.removeItem('pdv_role');
            sessionStorage.removeItem('pdv_user');


            // Back button use pannina admin page
            // history-la direct-a return aaga koodathu.
            window.location.replace(
                '/html/login.html'
            );
        }
    );
}
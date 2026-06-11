//// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
//// for details on configuring this project to bundle and minification
//// for details on configuring this project to bundle and minify static web assets.

//// Write your JavaScript code.

//// ============================================
//// MOBILE RESPONSIVE FUNCTIONALITY
//// ============================================

//document.addEventListener('DOMContentLoaded', function() {
//    // Mobile Sidebar Toggle
//    const openSidebarBtn = document.getElementById('openSidebarBtn');
//    const closeSidebarBtn = document.getElementById('closeSidebarBtn');
//    const sidebar = document.getElementById('mobileSidebar');

//    if (openSidebarBtn && sidebar) {
//        openSidebarBtn.addEventListener('click', function() {
//            sidebar.classList.add('show');
//            closeSidebarBtn.style.display = 'block';
//            // Prevent body scroll when sidebar is open
//            document.body.style.overflow = 'hidden';
//        });
//    }

//    if (closeSidebarBtn && sidebar) {
//        closeSidebarBtn.addEventListener('click', function() {
//            sidebar.classList.remove('show');
//            closeSidebarBtn.style.display = 'none';
//            // Re-enable body scroll
//            document.body.style.overflow = 'auto';
//        });
//    }

//    // Close sidebar when clicking on a nav link
//    const navLinks = document.querySelectorAll('.nav-link-custom');
//    navLinks.forEach(link => {
//        link.addEventListener('click', function() {
//            if (sidebar && window.innerWidth <= 768) {
//                sidebar.classList.remove('show');
//                closeSidebarBtn.style.display = 'none';
//                document.body.style.overflow = 'auto';
//            }
//        });
//    });

//    // Close sidebar when clicking outside on mobile
//    document.addEventListener('click', function(event) {
//        if (sidebar && 
//            !sidebar.contains(event.target) && 
//            !openSidebarBtn.contains(event.target) && 
//            window.innerWidth <= 768 &&
//            sidebar.classList.contains('show')) {
//            sidebar.classList.remove('show');
//            closeSidebarBtn.style.display = 'none';
//            document.body.style.overflow = 'auto';
//        }
//    });

//    // Handle window resize to show/hide menu button
//    function handleResize() {
//        if (window.innerWidth > 768) {
//            openSidebarBtn.style.display = 'none';
//            closeSidebarBtn.style.display = 'none';
//            sidebar.classList.remove('show');
//            document.body.style.overflow = 'auto';
//        } else {
//            if (sidebar.classList.contains('show')) {
//                openSidebarBtn.style.display = 'none';
//                closeSidebarBtn.style.display = 'block';
//            }
//        }
//    }

//    window.addEventListener('resize', handleResize);

//    // Initialize on load
//    handleResize();
//});
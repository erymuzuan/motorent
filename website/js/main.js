/**
 * Safe & Go Marketing Website - Main JavaScript
 */

(function() {
  'use strict';

  // ==================== DOM Ready ====================
  document.addEventListener('DOMContentLoaded', function() {
    initNavbar();
    initMobileNav();
    initFaq();
    initContactForm();
    initSmoothScroll();
    initAnimations();
    initCarousel();
  });

  // ==================== Navbar Scroll Effect ====================
  function initNavbar() {
    const navbar = document.getElementById('navbar');
    if (!navbar) return;

    // Only apply scroll effect on pages with hero (landing page)
    const isLandingPage = document.querySelector('.hero');
    if (!isLandingPage) return;

    function handleScroll() {
      if (window.scrollY > 50) {
        navbar.classList.add('scrolled');
      } else {
        navbar.classList.remove('scrolled');
      }
    }

    window.addEventListener('scroll', handleScroll, { passive: true });
    handleScroll(); // Initial check
  }

  // ==================== Mobile Navigation ====================
  function initMobileNav() {
    const navToggle = document.getElementById('navToggle');
    const navMenu = document.getElementById('navMenu');
    const navOverlay = document.getElementById('navOverlay');

    if (!navToggle || !navMenu) return;

    function openNav() {
      navMenu.classList.add('open');
      if (navOverlay) navOverlay.classList.add('open');
      document.body.style.overflow = 'hidden';
    }

    function closeNav() {
      navMenu.classList.remove('open');
      if (navOverlay) navOverlay.classList.remove('open');
      document.body.style.overflow = '';
    }

    navToggle.addEventListener('click', function(e) {
      e.preventDefault();
      if (navMenu.classList.contains('open')) {
        closeNav();
      } else {
        openNav();
      }
    });

    if (navOverlay) {
      navOverlay.addEventListener('click', closeNav);
    }

    // Close nav when clicking a link
    const navLinks = navMenu.querySelectorAll('a');
    navLinks.forEach(function(link) {
      link.addEventListener('click', closeNav);
    });

    // Close nav on escape key
    document.addEventListener('keydown', function(e) {
      if (e.key === 'Escape' && navMenu.classList.contains('open')) {
        closeNav();
      }
    });

    // Close nav on resize to desktop
    window.addEventListener('resize', function() {
      if (window.innerWidth >= 1024) {
        closeNav();
      }
    });
  }

  // ==================== FAQ Accordion ====================
  function initFaq() {
    const faqItems = document.querySelectorAll('.faq-item');

    faqItems.forEach(function(item) {
      const question = item.querySelector('.faq-question');
      if (!question) return;

      question.addEventListener('click', function() {
        // Close other open items
        faqItems.forEach(function(otherItem) {
          if (otherItem !== item && otherItem.classList.contains('open')) {
            otherItem.classList.remove('open');
          }
        });

        // Toggle current item
        item.classList.toggle('open');
      });
    });
  }

  // ==================== Contact Form ====================
  function initContactForm() {
    const form = document.getElementById('contactForm');
    if (!form) return;

    form.addEventListener('submit', function(e) {
      e.preventDefault();

      // Basic validation
      const requiredFields = form.querySelectorAll('[required]');
      let isValid = true;

      requiredFields.forEach(function(field) {
        // Remove existing error
        const existingError = field.parentNode.querySelector('.form-error');
        if (existingError) {
          existingError.remove();
        }
        field.style.borderColor = '';

        if (!field.value.trim()) {
          isValid = false;
          showFieldError(field, 'กรุณากรอกข้อมูล');
        } else if (field.type === 'email' && !isValidEmail(field.value)) {
          isValid = false;
          showFieldError(field, 'กรุณากรอกอีเมลที่ถูกต้อง');
        } else if (field.type === 'tel' && !isValidPhone(field.value)) {
          isValid = false;
          showFieldError(field, 'กรุณากรอกเบอร์โทรศัพท์ที่ถูกต้อง');
        }
      });

      if (isValid) {
        // Show success message
        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.textContent;
        submitBtn.textContent = 'กำลังส่ง...';
        submitBtn.disabled = true;

        // Simulate form submission
        setTimeout(function() {
          // In production, replace with actual form submission
          alert('ขอบคุณสำหรับข้อความ! เราจะติดต่อกลับโดยเร็วที่สุด');
          form.reset();
          submitBtn.textContent = originalText;
          submitBtn.disabled = false;
        }, 1500);
      }
    });

    function showFieldError(field, message) {
      field.style.borderColor = '#EF4444';
      const error = document.createElement('span');
      error.className = 'form-error';
      error.textContent = message;
      field.parentNode.appendChild(error);
    }

    function isValidEmail(email) {
      return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    function isValidPhone(phone) {
      // Thai phone number format
      return /^[0-9]{9,10}$/.test(phone.replace(/[-\s]/g, ''));
    }
  }

  // ==================== Smooth Scroll ====================
  function initSmoothScroll() {
    // Handle anchor links
    document.querySelectorAll('a[href^="#"]').forEach(function(anchor) {
      anchor.addEventListener('click', function(e) {
        const href = this.getAttribute('href');
        if (href === '#') return;

        const target = document.querySelector(href);
        if (target) {
          e.preventDefault();
          const headerOffset = 80;
          const elementPosition = target.getBoundingClientRect().top;
          const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

          window.scrollTo({
            top: offsetPosition,
            behavior: 'smooth'
          });
        }
      });
    });
  }

  // ==================== Scroll Animations ====================
  function initAnimations() {
    // Simple fade-in on scroll
    const animatedElements = document.querySelectorAll('.feature-card, .pricing-card, .testimonial-card, .step');

    if (!animatedElements.length) return;

    const observerOptions = {
      threshold: 0.1,
      rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function(entries) {
      entries.forEach(function(entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add('animate-fade-in-up');
          observer.unobserve(entry.target);
        }
      });
    }, observerOptions);

    animatedElements.forEach(function(el) {
      el.style.opacity = '0';
      observer.observe(el);
    });
  }

  // ==================== Carousel Component ====================
  function initCarousel() {
    const track = document.querySelector('.carousel-track');
    if (!track) return;

    const slides = Array.from(track.children);
    const nextButton = document.querySelector('.carousel-btn.next');
    const prevButton = document.querySelector('.carousel-btn.prev');
    const dotsNav = document.querySelector('.carousel-nav');
    if (!dotsNav) return;
    const dots = Array.from(dotsNav.children);

    const setSlidePosition = () => {
      const slideWidth = slides[0].getBoundingClientRect().width;
      slides.forEach((slide, index) => {
        slide.style.left = slideWidth * index + 'px';
      });
    };
    
    setSlidePosition();
    window.addEventListener('resize', setSlidePosition);

    const moveToSlide = (track, currentSlide, targetSlide) => {
      track.style.transform = 'translateX(-' + targetSlide.style.left + ')';
      currentSlide.classList.remove('current-slide');
      targetSlide.classList.add('current-slide');
    };

    const updateDots = (currentDot, targetDot) => {
      currentDot.classList.remove('current-slide');
      targetDot.classList.add('current-slide');
    };

    prevButton.addEventListener('click', e => {
      const currentSlide = track.querySelector('.current-slide') || slides[0];
      const prevSlide = currentSlide.previousElementSibling || slides[slides.length - 1];
      const currentDot = dotsNav.querySelector('.current-slide');
      const prevDot = currentDot.previousElementSibling || dots[dots.length - 1];

      moveToSlide(track, currentSlide, prevSlide);
      updateDots(currentDot, prevDot);
    });

    nextButton.addEventListener('click', e => {
      const currentSlide = track.querySelector('.current-slide') || slides[0];
      const nextSlide = currentSlide.nextElementSibling || slides[0];
      const currentDot = dotsNav.querySelector('.current-slide');
      const nextDot = currentDot.nextElementSibling || dots[0];

      moveToSlide(track, currentSlide, nextSlide);
      updateDots(currentDot, nextDot);
    });

    dotsNav.addEventListener('click', e => {
      const targetDot = e.target.closest('button');
      if (!targetDot) return;

      const currentSlide = track.querySelector('.current-slide') || slides[0];
      const currentDot = dotsNav.querySelector('.current-slide');
      const targetIndex = dots.findIndex(dot => dot === targetDot);
      const targetSlide = slides[targetIndex];

      moveToSlide(track, currentSlide, targetSlide);
      updateDots(currentDot, targetDot);
    });

    // Autoplay
    let autoplayInterval = 5000;
    let autoplay = setInterval(() => {
      nextButton.click();
    }, autoplayInterval);

    const carouselSection = document.querySelector('.carousel-section');
    carouselSection.addEventListener('mouseenter', () => clearInterval(autoplay));
    carouselSection.addEventListener('mouseleave', () => {
      autoplay = setInterval(() => {
        nextButton.click();
      }, autoplayInterval);
    });
  }

})();
